using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class AllVideosPageViewModel : ObservableRecipient,
    IRecipient<LibraryContentChangedMessage>
{
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<MediaViewModel> Videos { get; }

    private readonly LibraryContext _libraryContext;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _timer;

    public AllVideosPageViewModel(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _timer = _dispatcherQueue.CreateTimer();
        Videos = new ObservableCollection<MediaViewModel>();

        IsActive = true;
    }

    public void Receive(LibraryContentChangedMessage message)
    {
        if (message.LibraryId != KnownLibraryId.Videos) return;
        _dispatcherQueue.TryEnqueue(UpdateVideos);
    }

    public void UpdateVideos()
    {
        IsLoading = _libraryContext.IsLoadingVideos;
        IReadOnlyList<MediaViewModel> videos = _libraryContext.Videos;
        if (videos.Count < 5000)
        {
            // Only sync when the number of items is low enough
            // Sync on too many items can cause UI hang
            Videos.SyncItems(videos);
        }
        else
        {
            Videos.Clear();
            foreach (MediaViewModel video in videos)
            {
                Videos.Add(video);
            }
        }

        // Progressively update when it's still loading
        if (IsLoading)
        {
            _timer.Debounce(UpdateVideos, TimeSpan.FromSeconds(5));
        }
        else
        {
            _timer.Stop();
        }
    }

    [RelayCommand]
    private void Play(MediaViewModel media)
    {
        if (Videos.Count == 0) return;
        Messenger.SendQueueAndPlay(media, Videos, true);
    }
}
