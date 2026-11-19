using System.Collections.Concurrent;
using System.Text.Json;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Photino.NET;

namespace MultiAudio;

internal sealed class AudioWindow(PhotinoWindow window) : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentQueue<(LineInMonitor Monitor, Exception? Error)> _stopped = new();
    private List<AudioEndpoint> _inputs = new();
    private List<AudioEndpoint> _outputs = new();
    private LineInMonitor? _monitor;
    private int _volume = 50;

    public void OnMessage(object? sender, string message)
    {
        try
        {
            Command? command = JsonSerializer.Deserialize<Command>(message, JsonOptions);
            switch (command?.Action)
            {
                case "refresh" when _monitor is null:
                    RefreshDevices(command.Input, command.Output);
                    break;
                case "start" when _monitor is null:
                    StartMonitoring(command.Input, command.Output);
                    break;
                case "stop":
                    Dispose();
                    SendState("Monitoring stopped.");
                    break;
                case "volume" when command.Volume is int volume && volume is >= 0 and <= 100:
                    _volume = volume;
                    _monitor?.SetVolume(volume / 100f);
                    break;
                case "poll":
                    CheckStopped();
                    break;
            }
        }
        catch (JsonException)
        {
            SendState("The audio command was invalid.");
        }
        catch (Exception error)
        {
            SendState(error.Message);
        }
    }

    private void RefreshDevices(string? inputId, string? outputId)
    {
        string status;
        try
        {
            using MMDeviceEnumerator enumerator = new();
            _inputs = AudioEndpoint.ListActive(enumerator, DataFlow.Capture);
            _outputs = AudioEndpoint.ListActive(enumerator, DataFlow.Render);
            List<AudioEndpoint> lineInputs = _inputs.FindAll(endpoint => endpoint.IsLineIn);
            using MMDevice? defaultOutput = enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                ? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                : null;

            if (!_inputs.Exists(endpoint => endpoint.Id == inputId))
            {
                inputId = lineInputs.Count == 1 ? lineInputs[0].Id : null;
            }
            if (!_outputs.Exists(endpoint => endpoint.Id == outputId))
            {
                outputId = defaultOutput?.ID;
            }

            status = _outputs.Count == 0
                ? "Connect your headphones, then refresh devices."
                : lineInputs.Count == 0
                    ? "Connect a source to Line In, then refresh devices."
                    : "Ready to monitor.";
        }
        catch (Exception error)
        {
            _inputs.Clear();
            _outputs.Clear();
            inputId = null;
            outputId = null;
            status = $"Could not list audio devices. {error.Message}";
        }

        Send(new { inputs = _inputs, outputs = _outputs, inputId, outputId, running = false, status });
    }

    private void StartMonitoring(string? inputId, string? outputId)
    {
        if (inputId is null || outputId is null ||
            !_inputs.Exists(endpoint => endpoint.Id == inputId) ||
            !_outputs.Exists(endpoint => endpoint.Id == outputId))
        {
            SendState("Select an input and an output.");
            return;
        }

        LineInMonitor monitor = new(inputId, outputId);
        monitor.Stopped += OnStreamStopped;
        _monitor = monitor;

        try
        {
            monitor.Start(_volume / 100f);
        }
        catch (Exception error)
        {
            Dispose();
            SendState($"Could not start monitoring. {error.Message}");
            return;
        }

        SendState("Monitoring is on.");
    }

    private void OnStreamStopped(object? sender, StoppedEventArgs args)
    {
        if (sender is LineInMonitor monitor)
        {
            _stopped.Enqueue((monitor, args.Exception));
        }
    }

    private void CheckStopped()
    {
        while (_stopped.TryDequeue(out (LineInMonitor Monitor, Exception? Error) stopped))
        {
            if (stopped.Monitor == _monitor)
            {
                Dispose();
                SendState(stopped.Error is Exception error
                    ? $"Monitoring stopped. {error.Message}"
                    : "Monitoring stopped. Check the audio devices and restart.");
            }
        }
    }

    private void SendState(string status) => Send(new { running = _monitor is not null, status });

    private void Send<T>(T payload) => window.SendWebMessage(JsonSerializer.Serialize(payload, JsonOptions));

    public void Dispose()
    {
        if (_monitor is LineInMonitor monitor)
        {
            _monitor = null;
            monitor.Stopped -= OnStreamStopped;
            monitor.Dispose();
        }
        _stopped.Clear();
    }

    private sealed record Command(string Action, string? Input, string? Output, int? Volume);
}
