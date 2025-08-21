using Windows.System.Profile;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods to get information about the system.
/// </summary>
public static partial class DeviceInfoHelper
{
    public static readonly string DeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;

    /// <summary>
    /// Gets whether the current device is a desktop/laptop/tablet.
    /// </summary>
    /// <returns><see langword="true"/> if the device is a desktop; otherwise, <see langword="false"/>.</returns>
    public static bool IsDesktop => DeviceFamily == "Windows.Desktop";

    /// <summary>
    /// Gets whether the current device is an Xbox.
    /// </summary>
    /// <returns><see langword="true"/> if the device is an Xbox; otherwise, <see langword="false"/>.</returns>
    public static bool IsXbox => DeviceFamily == "Windows.Xbox";
}
