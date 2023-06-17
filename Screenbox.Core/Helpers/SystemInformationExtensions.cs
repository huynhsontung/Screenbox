using Microsoft.Toolkit.Uwp.Helpers;

namespace Screenbox.Core.Helpers;

public static class SystemInformationExtensions
{
    public static bool IsDesktop => SystemInformation.Instance.DeviceFamily == "Windows.Desktop";

    public static bool IsXbox => SystemInformation.Instance.DeviceFamily == "Windows.Xbox";
}
