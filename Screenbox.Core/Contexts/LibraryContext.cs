#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Models;
using Windows.Storage;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Holds the state for library management operations
/// </summary>
public sealed partial class LibraryContext : ObservableRecipient
{
    [ObservableProperty]
    public partial StorageLibrary? MusicStorageLibrary { get; set; }

    [ObservableProperty]
    public partial StorageLibrary? VideosStorageLibrary { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingVideos { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMusic { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial MusicLibrary Music { get; set; } = MusicLibrary.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial VideosLibrary Videos { get; set; } = VideosLibrary.Empty;
}

