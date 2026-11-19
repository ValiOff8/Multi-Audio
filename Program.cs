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
        using Stream icon = typeof(Program).Assembly.GetManifestResourceStream("MultiAudio.Icon")
            ?? throw new InvalidOperationException("The embedded icon is missing.");
        using MemoryStream iconData = new();
        icon.CopyTo(iconData);
        string html = reader.ReadToEnd().Replace(
            "{{APP_ICON_BASE64}}",
            Convert.ToBase64String(iconData.ToArray()),
            StringComparison.Ordinal);

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
        window.LoadRawString(html);
        window.WaitForClose();
    }
}
