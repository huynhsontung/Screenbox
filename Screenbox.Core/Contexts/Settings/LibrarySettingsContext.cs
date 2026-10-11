using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for media library settings.
/// </summary>
public sealed partial class LibrarySettingsContext : ObservableRecipient
{
    private const string UseIndexerKey = "Libraries/UseIndexer";
    private const string SearchRemovableStorageKey = "Libraries/SearchRemovableStorage";

    private readonly ISettingsStorage _storage;
    private readonly bool _isInitialized;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool UseIndexer { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool SearchRemovableStorage { get; set; }

    public LibrarySettingsContext(ISettingsStorage storage)
    {
        _storage = storage;
        UseIndexer = _storage.GetValue(UseIndexerKey, true);
        SearchRemovableStorage = _storage.GetValue(SearchRemovableStorageKey, true);
        _isInitialized = true;
        IsActive = true;
    }

    partial void OnUseIndexerChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(UseIndexerKey, value);
    }

    partial void OnSearchRemovableStorageChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(SearchRemovableStorageKey, value);
    }
}
