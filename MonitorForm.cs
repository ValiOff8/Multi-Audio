using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MultiAudio;

internal sealed class MonitorForm : Form
{
    private readonly ComboBox _input = CreateDevicePicker("Input");
    private readonly ComboBox _output = CreateDevicePicker("Output");
    private readonly TrackBar _volume = new()
    {
        AccessibleName = "Line-in volume",
        Dock = DockStyle.Top,
        Maximum = 100,
        Value = 50,
        TickFrequency = 10
    };
    private readonly Label _volumeLabel = new() { Text = "Line-in volume (50%)", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, Dock = DockStyle.Top };
    private readonly Button _toggle = new() { Text = "Start", AutoSize = true };
    private readonly Button _refresh = new() { Text = "Refresh devices", AutoSize = true };
    private LineInMonitor? _monitor;

    public MonitorForm()
    {
        Text = "Multi-Audio";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new(500, 290);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AcceptButton = _toggle;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new(16)
        };
        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        Controls.Add(layout);

        FlowLayoutPanel actions = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top
        };
        actions.Controls.Add(_toggle);
        actions.Controls.Add(_refresh);

        Control[] rows =
        [
            new Label { Text = "Input", AutoSize = true }, _input,
            new Label { Text = "Output", AutoSize = true }, _output,
            _volumeLabel, _volume, actions, _status
        ];
        for (int row = 0; row < rows.Length; row++)
        {
            layout.RowStyles.Add(new(SizeType.AutoSize));
            layout.Controls.Add(rows[row], 0, row);
        }

        _input.SelectedIndexChanged += (_, _) => UpdateControls();
        _output.SelectedIndexChanged += (_, _) => UpdateControls();
        _volume.ValueChanged += (_, _) =>
        {
            _volumeLabel.Text = $"Line-in volume ({_volume.Value}%)";
            _monitor?.SetVolume(_volume.Value / 100f);
        };
        _toggle.Click += (_, _) => ToggleMonitoring();
        _refresh.Click += (_, _) => RefreshDevices();
        RefreshDevices();
    }

    private static ComboBox CreateDevicePicker(string name) => new()
    {
        AccessibleName = name,
        DisplayMember = nameof(AudioEndpoint.Name),
        Dock = DockStyle.Top,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Margin = new(3, 3, 3, 10)
    };

    private void RefreshDevices()
    {
        try
        {
            using MMDeviceEnumerator enumerator = new();
            List<AudioEndpoint> inputs = AudioEndpoint.ListActive(enumerator, DataFlow.Capture);
            List<AudioEndpoint> outputs = AudioEndpoint.ListActive(enumerator, DataFlow.Render);
            List<AudioEndpoint> lineInputs = inputs.FindAll(endpoint => endpoint.IsLineIn);
            using MMDevice? defaultOutput = enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                ? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                : null;

            FillPicker(_input, inputs, lineInputs.Count == 1 ? lineInputs[0].Id : null);
            FillPicker(_output, outputs, defaultOutput?.ID);

            _status.Text = lineInputs.Count == 0
                ? "Connect a source to the blue Line In jack, then click Refresh devices."
                : string.Empty;
            if (outputs.Count == 0)
            {
                _status.Text = "Connect your headphones, then click Refresh devices.";
            }
        }
        catch (Exception error)
        {
            _input.Items.Clear();
            _output.Items.Clear();
            _status.Text = $"Could not list audio devices. {error.Message}";
        }

        UpdateControls();
    }

    private static void FillPicker(ComboBox picker, List<AudioEndpoint> endpoints, string? preferredId)
    {
        string? selectedId = (picker.SelectedItem as AudioEndpoint)?.Id;
        picker.Items.Clear();
        picker.Items.AddRange(endpoints.ToArray());
        int previousIndex = endpoints.FindIndex(endpoint => endpoint.Id == selectedId);
        picker.SelectedIndex = previousIndex >= 0
            ? previousIndex
            : endpoints.FindIndex(endpoint => endpoint.Id == preferredId);
    }

    private void ToggleMonitoring()
    {
        if (_monitor is not null)
        {
            StopMonitoring();
            _status.Text = "Monitoring stopped.";
            return;
        }

        if (_input.SelectedItem is not AudioEndpoint input || _output.SelectedItem is not AudioEndpoint output)
        {
            return;
        }

        LineInMonitor monitor = new(input.Id, output.Id);
        _monitor = monitor;
        monitor.Stopped += OnMonitoringStopped;

        try
        {
            monitor.Start(_volume.Value / 100f);
            _status.Text = "Monitoring is on.";
        }
        catch (Exception error)
        {
            StopMonitoring();
            _status.Text = $"Could not start monitoring. {error.Message}";
        }

        UpdateControls();
    }

    private void OnMonitoringStopped(object? sender, StoppedEventArgs args)
    {
        if (sender != _monitor)
        {
            return;
        }

        StopMonitoring();
        _status.Text = args.Exception is Exception error
            ? $"Monitoring stopped. {error.Message}"
            : "Monitoring stopped. Check the audio devices and restart.";
    }

    private void StopMonitoring()
    {
        if (_monitor is LineInMonitor monitor)
        {
            _monitor = null;
            monitor.Stopped -= OnMonitoringStopped;
            monitor.Dispose();
        }

        UpdateControls();
    }

    private void UpdateControls()
    {
        bool running = _monitor is not null;
        _input.Enabled = _output.Enabled = _refresh.Enabled = !running;
        _toggle.Enabled = running || (_input.SelectedItem is not null && _output.SelectedItem is not null);
        _toggle.Text = running ? "Stop" : "Start";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        StopMonitoring();
        base.OnFormClosed(e);
    }
}
