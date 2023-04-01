using Screenbox.Core.Enums;

namespace Screenbox.Core.Services
{
    public interface IResourceService
    {
        string GetString(ResourceName name, params object[] parameters);
    }
}
