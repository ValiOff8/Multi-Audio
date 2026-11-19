using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MultiAudio;

internal sealed class LineInMonitor(string inputId, string outputId) : IDisposable
{
    private MMDevice? _inputDevice;
    private MMDevice? _outputDevice;
    private WasapiCapture? _capture;
    private WasapiOut? _playback;
    private VolumeSampleProvider? _volume;

    public event EventHandler<StoppedEventArgs>? Stopped;

    public void Start(float volume)
    {
        using MMDeviceEnumerator enumerator = new();
        _inputDevice = enumerator.GetDevice(inputId);
        _outputDevice = enumerator.GetDevice(outputId);

        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48_000, 2);
        BufferedWaveProvider buffer = new(format)
        {
            BufferDuration = TimeSpan.FromMilliseconds(120),
            DiscardOnBufferOverflow = true,
            ReadFully = true
        };
        _volume = new VolumeSampleProvider(buffer.ToSampleProvider()) { Volume = volume };

        _capture = new(_inputDevice, true, 30)
        {
            WaveFormat = format,
            ShareMode = AudioClientShareMode.Shared
        };
        _capture.DataAvailable += (_, args) => buffer.AddSamples(args.Buffer, 0, args.BytesRecorded);
        _capture.RecordingStopped += OnStreamStopped;

        _playback = new(_outputDevice, AudioClientShareMode.Shared, true, 30);
        _playback.PlaybackStopped += OnStreamStopped;
        _playback.Init(_volume.ToWaveProvider());

        _capture.StartRecording();
        _playback.Play();
    }

    public void SetVolume(float volume)
    {
        if (_volume is not null)
        {
            _volume.Volume = volume;
        }
    }

    private void OnStreamStopped(object? sender, StoppedEventArgs args)
    {
        Stopped?.Invoke(this, args);
    }

    public void Dispose()
    {
        using MMDevice? inputDevice = _inputDevice;
        using MMDevice? outputDevice = _outputDevice;
        using WasapiOut? playback = _playback;
        using WasapiCapture? capture = _capture;
        _inputDevice = null;
        _outputDevice = null;
        _playback = null;
        _capture = null;
        _volume = null;

        if (capture is not null)
        {
            capture.RecordingStopped -= OnStreamStopped;
        }

        if (playback is not null)
        {
            playback.PlaybackStopped -= OnStreamStopped;
        }
    }
}
