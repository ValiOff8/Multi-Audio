# Multi-Audio

Multi-Audio plays a Windows Line In through the same output as your PC audio. Windows mixes both audio streams. The app keeps the input audio in memory and plays it locally.

## Run on Windows

1. Keep your headphones plugged into the front jack. Connect the other device's audio output to the blue Line In jack on the rear of the PC.
2. In Windows sound settings, select your front headphones as the output.
3. Open `MultiAudio.exe`, confirm that **Input** is **Line In** and **Output** is the same headphone device, and click **Start**. The **Line-in volume** slider adjusts the incoming audio.

If Line In is missing, plug in the cable and click **Refresh devices**. If it still does not appear, run `mmsys.cpl`, open the **Recording** tab, and check whether Line In is disabled.

The single-file executable targets 64-bit Windows and includes the .NET runtime. The Photino interface uses the Microsoft Edge WebView2 Runtime installed on Windows.

## Build

Run this command from the directory containing `MultiAudio.csproj` with the .NET 10 SDK.

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o dist/photino
```
