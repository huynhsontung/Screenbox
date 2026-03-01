#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    public ObservableCollection<MediaViewModel> Items { get; } = new();

    public double ItemsCount => Items.Count;    // For binding

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private object? _thumbnail;
    [ObservableProperty] private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
    [ObservableProperty] private BitmapImage? _thumbnail1;
    [ObservableProperty] private BitmapImage? _thumbnail2;
    [ObservableProperty] private BitmapImage? _thumbnail3;
    [ObservableProperty] private BitmapImage? _thumbnail4;

    /// <summary>Gets whether at least one thumbnail image is loaded for the collage.</summary>
    public bool HasThumbnails => Thumbnail1 != null || Thumbnail2 != null || Thumbnail3 != null || Thumbnail4 != null;

    public string Id => _id.ToString();

    private Guid _id = Guid.NewGuid();

    private readonly IPlaylistService _playlistService;
    private readonly MediaViewModelFactory _mediaFactory;

    public PlaylistViewModel(IPlaylistService playlistService, MediaViewModelFactory mediaFactory)
    {
        _playlistService = playlistService;
        _mediaFactory = mediaFactory;

        Items.CollectionChanged += Items_CollectionChanged;
    }

    private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ItemsCount));
    }

    public Playlist ToPlaylist()
    {
        return new Playlist(0, Items);
    }

    public void Load(PersistentPlaylist persistentPlaylist)
    {
        if (!Guid.TryParse(persistentPlaylist.Id, out _id)) return;
        Name = persistentPlaylist.DisplayName;
        LastUpdated = persistentPlaylist.LastUpdated;
        Items.Clear();
        foreach (var item in persistentPlaylist.Items)
        {
            try
            {
                var vm = ToMediaViewModel(item);
                Items.Add(Items.Contains(vm) ? new MediaViewModel(vm) : vm);
            }
            catch { }
        }
    }

    /// <summary>Loads thumbnails for the first 4 items to populate the collage.</summary>
    public async Task LoadThumbnailsAsync()
    {
        var itemsToLoad = Items.Take(4).ToList();
        await Task.WhenAll(itemsToLoad.Select(m => m.LoadThumbnailAsync()));

        Thumbnail1 = itemsToLoad.Count > 0 ? itemsToLoad[0].Thumbnail : null;
        Thumbnail2 = itemsToLoad.Count > 1 ? itemsToLoad[1].Thumbnail : null;
        Thumbnail3 = itemsToLoad.Count > 2 ? itemsToLoad[2].Thumbnail : null;
        Thumbnail4 = itemsToLoad.Count > 3 ? itemsToLoad[3].Thumbnail : null;
        OnPropertyChanged(nameof(HasThumbnails));
    }

    public async Task SaveAsync()
    {
        LastUpdated = DateTimeOffset.Now;
        var persistentPlaylist = ToPersistentPlaylist();
        await _playlistService.SavePlaylistAsync(persistentPlaylist);
    }

    public async Task RenameAsync(string newDisplayName)
    {
        Name = newDisplayName;
        await SaveAsync();
    }

    [RelayCommand]
    public async Task AddItemsAsync(IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;
        foreach (var item in items)
        {
            Items.Add(Items.Contains(item) ? new MediaViewModel(item) : item);
        }

        await SaveAsync();
    }

    internal PersistentPlaylist ToPersistentPlaylist()
    {
        return new PersistentPlaylist
        {
            Id = _id.ToString(),
            DisplayName = Name,
            LastUpdated = LastUpdated,
            Items = Items.Select(m => new PersistentMediaRecord(
                m.Name,
                m.Location,
                m.MediaType == MediaPlaybackType.Music ? m.MediaInfo.MusicProperties : m.MediaInfo.VideoProperties,
                m.DateAdded
            )).ToList()
        };
    }

    private MediaViewModel ToMediaViewModel(PersistentMediaRecord record)
    {
        MediaViewModel media;
        if (Uri.TryCreate(record.Path, UriKind.Absolute, out var uri))
        {
            media = _mediaFactory.GetSingleton(uri);
        }
        else
        {
            media = _mediaFactory.GetTransient(new Uri("about:blank"));
            media.IsAvailable = false;
        }

        if (!string.IsNullOrEmpty(record.Title))
            media.Name = record.Title;

        media.MediaInfo = record.Properties != null
            ? new MediaInfo(record.Properties)
            : new MediaInfo(record.MediaType, record.Title, record.Year, record.Duration);

        if (record.DateAdded != default)
        {
            DateTimeOffset utcTime = DateTime.SpecifyKind(record.DateAdded, DateTimeKind.Utc);
            media.DateAdded = utcTime.ToLocalTime();
        }

        return media;
    }
}

