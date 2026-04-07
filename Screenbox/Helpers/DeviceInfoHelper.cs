#nullable enable

using System;
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

    /// <summary>
    /// Gets the type of device reported by the system.
    /// </summary>
    /// <value>A string representing the device family.</value>
    public static readonly string DeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;

    /// <summary>
    /// Gets a value that indicates whether the current device is a desktop/laptop/tablet.
    /// </summary>
    /// <value><see langword="true"/> if the device is a desktop; otherwise, <see langword="false"/>.</value>
    public static readonly bool IsDesktop = string.Equals(DeviceFamily, "Windows.Desktop", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value that indicates whether the current device is an Xbox.
    /// </summary>
    /// <value><see langword="true"/> if the device is an Xbox; otherwise, <see langword="false"/>.</value>
    public static readonly bool IsXbox =
        string.Equals(DeviceFamily, "Windows.Xbox", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(DeviceFamily, "Windows.XBoxSRA", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(DeviceFamily, "Windows.XBoxERA", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value that indicates whether the current device has a physical keyboard.
    /// </summary>
    /// <value><see langword="true"/> if a keyboard is detected; otherwise, <see langword="false"/>.</value>
    public static bool IsKeyboardPresent => _keyboardCapabilities.KeyboardPresent != 0;

    /// <summary>
    /// Gets a value that indicates whether the current device has a touch digitizer.
    /// </summary>
    /// <value><see langword="true"/> if touch input is supported; otherwise, <see langword="false"/>.</value>
    public static bool IsTouchPresent => _touchCapabilities.TouchPresent != 0;
}
