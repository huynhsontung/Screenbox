using System.Runtime.InteropServices;

namespace Screenbox.Helpers;

/// <summary>
/// Provides native interop methods and helpers to detect touchpad input and configure thread touchpad capabilities.
/// </summary>
internal static partial class TouchpadHelper
{
    internal enum InputMessageDeviceType
    {
        Unavailable = 0,
        Keyboard = 1,
        Mouse = 2,
        Touch = 3,
        Pen = 4,
        Touchpad = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InputMessageSource
    {
        public InputMessageDeviceType DeviceType;
        public int OriginId;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterTouchpadCapableThread([MarshalAs(UnmanagedType.Bool)] bool fEnable);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCurrentInputMessageSource(out InputMessageSource inputMessageSource);

    /// <summary>
    /// Registers the current thread as touchpad capable if running on Windows Desktop.
    /// </summary>
    /// <remarks>
    /// This enables <see cref="GetCurrentInputMessageSource"/> to identify Precision Touchpad inputs
    /// as <see cref="InputMessageDeviceType.Touchpad"/> instead of defaulting to mouse emulation.
    /// On non-desktop devices (such as Xbox), this operation is safely skipped to avoid missing entry point errors.
    /// </remarks>
    /// <returns><see langword="true"/> if registration succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool RegisterTouchpadCapableThread()
    {
        if (!DeviceInfoHelper.IsDesktop)
        {
            return false;
        }

        return RegisterTouchpadCapableThread(true);
    }

    /// <summary>
    /// Checks if the current input message being processed originated from a Precision Touchpad.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the current message source is a touchpad; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsCurrentInputFromTouchpad()
    {
        if (!DeviceInfoHelper.IsDesktop)
        {
            return false;
        }

        if (GetCurrentInputMessageSource(out InputMessageSource source))
        {
            return source.DeviceType is InputMessageDeviceType.Touchpad;
        }

        return false;
    }
}
