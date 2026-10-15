using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for general application behavior.
/// </summary>
public sealed partial class GeneralSettingsContext : SettingsContextBase
{
    private const string GeneralEnqueueAllInFolderKey = "General/EnqueueAllInFolder";

    public bool EnqueueAllFilesInFolder
    {
        get => Get(GeneralEnqueueAllInFolderKey, false);
        set => Set(GeneralEnqueueAllInFolderKey, value);
    }

    public GeneralSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
