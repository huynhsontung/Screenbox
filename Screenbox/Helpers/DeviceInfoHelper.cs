using Windows.Devices.Input;
using Windows.System.Profile;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods to get information about the system.
/// </summary>
public static class DeviceInfoHelper
{
    private static readonly KeyboardCapabilities _keyboardCapabilities = new();
    private static readonly TouchCapabilities _touchCapabilities = new();

    public static readonly string DeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;

    /// <summary>
    /// Gets whether the current device is a desktop/laptop/tablet.
    /// </summary>
    /// <returns><see langword="true"/> if the device is a desktop; otherwise, <see langword="false"/>.</returns>
    public static readonly bool IsDesktop = DeviceFamily == "Windows.Desktop";

    /// <summary>
    /// Gets whether the current device is an Xbox.
    /// </summary>
    /// <returns><see langword="true"/> if the device is an Xbox; otherwise, <see langword="false"/>.</returns>
    public static readonly bool IsXbox = (DeviceFamily == "Windows.Xbox") || (DeviceFamily == "Windows.XBoxSRA") || (DeviceFamily == "Windows.XBoxERA");

    /// <summary>
    /// Gets whether the current device has a physical keyboard connected.
    /// </summary>
    /// <returns><see langword="true"/> if a keyboard is detected; otherwise, <see langword="false"/>.</returns>
    public static bool IsKeyboardPresent => _keyboardCapabilities.KeyboardPresent != 0;

    /// <summary>
    /// Gets whether the current device has a touch digitizer.
    /// </summary>
    /// <returns><see langword="true"/> if touch input is supported; otherwise, <see langword="false"/>.</returns>
    public static bool IsTouchPresent => _touchCapabilities.TouchPresent != 0;
}
