using System.Runtime.Versioning;
using Windows.Foundation.Metadata;

namespace Screenbox.Core.Helpers;

[SupportedOSPlatform("windows10.0.10240")]
internal static class KnownFolderAccess
{
    [SupportedOSPlatformGuard("windows10.0.19041")]
    internal static bool IsRequestAccessSupported =>
        ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 10);
}
