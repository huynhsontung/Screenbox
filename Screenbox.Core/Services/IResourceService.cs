using Screenbox.Core.Enums;

namespace Screenbox.Core.Services
{
    public interface IResourceService
    {
        string GetString(ResourceName name, params object[] parameters);
        string GetString(PluralResourceName name, double count, params object[] parameters);
    }
}
