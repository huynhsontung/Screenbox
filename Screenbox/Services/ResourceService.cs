using CommunityToolkit.Diagnostics;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Screenbox.Strings;
using System;

namespace Screenbox.Services
{
    public sealed class ResourceService : IResourceService
    {
        public string GetString(ResourceName name, params object[] parameters)
        {
            switch (name)
            {
                case ResourceName.GoToPosition:
                    Guard.HasSizeGreaterThanOrEqualTo(parameters, 1);
                    return Resources.GoToPosition((string)parameters[0]);
                case ResourceName.VolumeChangeStatusMessage:
                    Guard.HasSizeGreaterThanOrEqualTo(parameters, 1);
                    return Resources.VolumeChangeStatusMessage(Convert.ToDouble(parameters[0]));
                case ResourceName.TrackIndex:
                    Guard.HasSizeGreaterThanOrEqualTo(parameters, 1);
                    return Resources.TrackIndex(Convert.ToInt32(parameters[0]));
                case ResourceName.SubtitleStatus:
                    Guard.HasSizeGreaterThanOrEqualTo(parameters, 1);
                    return Resources.SubtitleStatus((string)parameters[0]);
                case ResourceName.ScaleStatus:
                    Guard.HasSizeGreaterThanOrEqualTo(parameters, 1);
                    return Resources.ScaleStatus((string)parameters[0]);
                default:
                    string resourceName = Enum.GetName(typeof(ResourceName), name) ??
                                       throw new ArgumentOutOfRangeException(nameof(name), name, null);
                    return typeof(Resources).GetProperty(resourceName, typeof(string))?.GetValue(null) as string ??
                           throw new ArgumentOutOfRangeException(nameof(resourceName), resourceName, "Invalid resource name");
            }
        }
    }
}
