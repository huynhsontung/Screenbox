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

namespace Screenbox.Core.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    public ObservableCollection<MediaViewModel> Items { get; } = new();

    public double ItemsCount => Items.Count;    // For binding

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private object? _thumbnail;
    [ObservableProperty] private DateTimeOffset _lastUpdated = DateTimeOffset.Now;

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

    public Playlist GetPlaylist()
    {
        return new Playlist(Items);
    }

    public void Load(PersistentPlaylist persistentPlaylist)
    {
        if (!Guid.TryParse(persistentPlaylist.Id, out _id)) return;
        Name = persistentPlaylist.DisplayName;
        LastUpdated = persistentPlaylist.LastUpdated;
        Items.Clear();
        foreach (var item in persistentPlaylist.Items)
        {
            var vm = ToMediaViewModel(item);
            Items.Add(Items.Contains(vm) ? new MediaViewModel(vm) : vm);
        }
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

    private PersistentPlaylist ToPersistentPlaylist()
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
        MediaViewModel media = _mediaFactory.GetSingleton(new Uri(record.Path));
        if (!string.IsNullOrEmpty(record.Title)) media.Name = record.Title;
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
