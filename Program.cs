using Photino.NET;

namespace MultiAudio;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using Stream frontend = typeof(Program).Assembly.GetManifestResourceStream("MultiAudio.Frontend")
            ?? throw new InvalidOperationException("The embedded frontend is missing.");
        using StreamReader reader = new(frontend);

        PhotinoWindow window = new PhotinoWindow()
            .SetTitle("Multi-Audio")
            .SetSize(530, 480)
            .SetMinSize(360, 420)
            .SetDevToolsEnabled(false)
            .SetContextMenuEnabled(false)
            .SetFileSystemAccessEnabled(false)
            .SetGrantBrowserPermissions(false);
        window.Centered = true;

        using AudioWindow audio = new(window);
        window.RegisterWebMessageReceivedHandler(audio.OnMessage);
        window.LoadRawString(reader.ReadToEnd());
        window.WaitForClose();
    }
}
