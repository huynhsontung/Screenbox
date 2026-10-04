using System.Runtime.Versioning;
using Windows.Foundation.Metadata;
using Windows.System.Profile;

namespace Screenbox.Core.Helpers;

public static class SystemInformation
{
    public static readonly string DeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;

    [SupportedOSPlatformGuard("windows10.0.19041")]
    #pragma warning disable CA1416
    public static bool IsKnownFolderRequestAccessSupported =>
        ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 10);
    #pragma warning restore CA1416

    public static bool IsDesktop => DeviceFamily == "Windows.Desktop";

    public static bool IsXbox => DeviceFamily == "Windows.Xbox";
}
