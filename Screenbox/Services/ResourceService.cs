using System;
using CommunityToolkit.Diagnostics;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Screenbox.Strings;

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
                default:
                    string valueName = Enum.GetName(typeof(ResourceName), name) ??
                                       throw new ArgumentOutOfRangeException(nameof(name), name, null);
                    return typeof(Resources).GetProperty(valueName)?.GetValue(null) as string ??
                           throw new ArgumentOutOfRangeException(nameof(valueName), valueName, "Incorrect resource name");
            }
        }
    }
}
