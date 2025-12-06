#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlaylistDetailsPageViewModel : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsCount))]
    [NotifyPropertyChangedFor(nameof(TotalDuration))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private PlaylistViewModel? _source;

    public string DisplayName => Source?.Caption ?? string.Empty;

    public int ItemsCount => Source?.Items.Count ?? 0;

    public TimeSpan TotalDuration => Source != null ? GetTotalDuration(Source.Items) : TimeSpan.Zero;

    public ObservableCollection<MediaViewModel> Items { get; }

    private List<MediaViewModel>? _itemList;
    private readonly IPlaylistService _playlistService;
    private readonly IFilesService _filesService;

    public PlaylistDetailsPageViewModel(IPlaylistService playlistService, IFilesService filesService)
    {
        _playlistService = playlistService;
        _filesService = filesService;
        Items = new ObservableCollection<MediaViewModel>();
    }

    public void OnNavigatedTo(object? parameter)
    {
        Source = parameter switch
        {
            NavigationMetadata { Parameter: PlaylistViewModel source } => source,
            PlaylistViewModel source => source,
            _ => throw new ArgumentException("Navigation parameter is not a playlist")
        };
    }

    partial void OnSourceChanged(PlaylistViewModel? value)
    {
        if (value == null)
        {
            Items.Clear();
            _itemList = null;
            return;
        }

        Items.Clear();
        foreach (MediaViewModel media in value.Items)
        {
            Items.Add(media);
        }
    }

    [RelayCommand]
    private void Play(MediaViewModel item)
    {
        _itemList ??= Items.ToList();
        Messenger.SendQueueAndPlay(item, _itemList);
    }

    [RelayCommand]
    private void ShuffleAndPlay()
    {
        if (Source == null || Source.Items.Count == 0) return;
        Random rnd = new();
        List<MediaViewModel> shuffledList = Source.Items.OrderBy(_ => rnd.Next()).ToList();
        var playlist = new Playlist(0, shuffledList);
        Messenger.Send(new QueuePlaylistMessage(playlist, true));
    }

    [RelayCommand]
    private void Remove(MediaViewModel item)
    {
        if (Source == null) return;
        Source.Items.Remove(item);
        Items.Remove(item);
        _itemList = null;
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(TotalDuration));
        _ = SavePlaylistAsync();
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        if (Source == null) return;

        IReadOnlyList<StorageFile>? files = await _filesService.PickMultipleFilesAsync();
        if (files == null || files.Count == 0) return;

        // TODO: Create media view models from files and add to playlist
        // For now, this is a placeholder

        await SavePlaylistAsync();
    }

    private async Task SavePlaylistAsync()
    {
        if (Source == null) return;

        Source.LastUpdated = DateTimeOffset.Now;
        var persistentPlaylist = Source.ToPersistentPlaylist();
        await _playlistService.SavePlaylistAsync(persistentPlaylist);
    }

    private static TimeSpan GetTotalDuration(IEnumerable<MediaViewModel> items)
    {
        TimeSpan duration = TimeSpan.Zero;
        foreach (MediaViewModel item in items)
        {
            duration += item.Duration;
        }

        return duration;
    }
}
