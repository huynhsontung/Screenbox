#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlaylistDetailsPageViewModel : ObservableRecipient
{
    [ObservableProperty]
    private PlaylistViewModel? _source;

    private List<MediaViewModel>? _itemList;
    private readonly IFilesService _filesService;
    private readonly IPlaylistService _playlistService;
    private readonly MediaViewModelFactory _mediaFactory;

    public PlaylistDetailsPageViewModel(IFilesService filesService, IPlaylistService playlistService, MediaViewModelFactory mediaFactory)
    {
        _filesService = filesService;
        _playlistService = playlistService;
        _mediaFactory = mediaFactory;
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

    [RelayCommand]
    private void Play(MediaViewModel item)
    {
        if (Source == null) return;
        _itemList ??= Source.Items.ToList();
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
    private async Task Remove(MediaViewModel item)
    {
        if (Source == null) return;
        Source.Items.Remove(item);
        _itemList = null;
        await Source.SaveAsync();
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        if (Source == null) return;

        IReadOnlyList<StorageFile>? files = await _filesService.PickMultipleFilesAsync();
        if (files == null || files.Count == 0) return;

        var mediaList = files.Where(f => f.IsSupported()).Select(_mediaFactory.GetSingleton).ToList();
        if (mediaList.Count == 0) return;

        foreach (var item in mediaList)
        {
            Source.Items.Add(item);
        }

        // Invalidate cached item list
        _itemList = null;

        // Save the updated playlist to disk
        await Source.SaveAsync();

        // Load media details in parallel
        await Task.WhenAll(mediaList.Select(m => m.LoadDetailsAsync(_filesService)));
    }

    public async Task<bool> DeletePlaylistAsync()
    {
        if (Source == null) return false;

        await _playlistService.DeletePlaylistAsync(Source.Id);
        return true;
    }
}
