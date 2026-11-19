using NAudio.CoreAudioApi;

namespace MultiAudio;

internal sealed record AudioEndpoint(string Id, string Name, bool IsLineIn)
{
    private const uint LineLevelFormFactor = 2;

    public static List<AudioEndpoint> ListActive(MMDeviceEnumerator enumerator, DataFlow flow)
    {
        List<AudioEndpoint> endpoints = new();

        foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            using (device)
            {
                string name = device.FriendlyName;
                bool isLineIn = flow == DataFlow.Capture &&
                    (device.Properties[PropertyKeys.PKEY_AudioEndpoint_FormFactor]?.Value is LineLevelFormFactor ||
                     name.StartsWith("Line In", StringComparison.OrdinalIgnoreCase) ||
                     name.StartsWith("Line-In", StringComparison.OrdinalIgnoreCase));

                endpoints.Add(new(device.ID, name, isLineIn));
            }
        }

        return endpoints;
    }
}
