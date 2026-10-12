using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for media library settings.
/// </summary>
public sealed partial class LibrarySettingsContext : SettingsContextBase
{
    private const string UseIndexerKey = "Libraries/UseIndexer";
    private const string SearchRemovableStorageKey = "Libraries/SearchRemovableStorage";

    public bool UseIndexer
    {
        get => Get(UseIndexerKey, true);
        set => Set(UseIndexerKey, value);
    }

    public bool SearchRemovableStorage
    {
        get => Get(SearchRemovableStorageKey, true);
        set => Set(SearchRemovableStorageKey, value);
    }

    public LibrarySettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
