#nullable enable

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    public ObservableCollection<MediaViewModel> Items { get; } = new();

    [ObservableProperty] private string _caption = string.Empty;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private object? _thumbnail;
    [ObservableProperty] private DateTimeOffset _lastUpdated = DateTimeOffset.Now;

    private Guid _id;

    private readonly IPlaylistService _playlistService;
    private readonly MediaViewModelFactory _mediaFactory;

    public PlaylistViewModel(IPlaylistService playlistService, MediaViewModelFactory mediaFactory)
    {
        _playlistService = playlistService;
        _mediaFactory = mediaFactory;
    }

    public Playlist GetPlaylist()
    {
        return new Playlist(Items);
    }

    public void Load(PersistentPlaylist persistentPlaylist)
    {
        if (!Guid.TryParse(persistentPlaylist.Id, out _id)) return;
        Caption = persistentPlaylist.DisplayName;
        LastUpdated = persistentPlaylist.LastUpdated;
        Items.Clear();
        foreach (var item in persistentPlaylist.Items)
        {
            Items.Add(ToMediaViewModel(item));
        }
    }

    private MediaViewModel ToMediaViewModel(PersistentMediaRecord record)
    {
        MediaViewModel media = _mediaFactory.GetSingleton(new Uri(record.Path));
        if (!string.IsNullOrEmpty(record.Title)) media.Name = record.Title;
        media.MediaInfo = new MediaInfo(record.Properties);
        if (record.DateAdded != default)
        {
            DateTimeOffset utcTime = DateTime.SpecifyKind(record.DateAdded, DateTimeKind.Utc);
            media.DateAdded = utcTime.ToLocalTime();
        }
        return media;
    }
}
