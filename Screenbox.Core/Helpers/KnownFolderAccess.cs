using System.Runtime.Versioning;
using Windows.Foundation.Metadata;

namespace Screenbox.Core.Helpers;

internal static class KnownFolderAccess
{
    [SupportedOSPlatformGuard("windows10.0.19041")]
    internal static bool IsRequestAccessSupported =>
        ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 10);
}
