using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for media library settings and sorting preferences.
/// </summary>
public sealed partial class LibrarySettingsContext : SettingsContextBase
{
    private const string UseIndexerKey = "Libraries/UseIndexer";
    private const string SearchRemovableStorageKey = "Libraries/SearchRemovableStorage";
    private const string PersistentSongsSortOrderKey = "Values/Songs/SortOrder";
    private const string PersistentAlbumsSortOrderKey = "Values/Albums/SortOrder";

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

    public SongSortOrder SongsSortOrder
    {
        get => Get(PersistentSongsSortOrderKey, SongSortOrder.Title);
        set => Set(PersistentSongsSortOrderKey, value);
    }

    public AlbumSortOrder AlbumsSortOrder
    {
        get => Get(PersistentAlbumsSortOrderKey, AlbumSortOrder.Title);
        set => Set(PersistentAlbumsSortOrderKey, value);
    }

    public LibrarySettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
