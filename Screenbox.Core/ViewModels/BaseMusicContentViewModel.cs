#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Screenbox.Core.ViewModels;
public abstract partial class BaseMusicContentViewModel : ObservableRecipient
{
    private bool HasSongs => Songs.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShuffleAndPlayCommand))]
    private IReadOnlyList<MediaViewModel> _songs = Array.Empty<MediaViewModel>();

    [ObservableProperty]
    private string _sortBy = string.Empty;

    [ObservableProperty] private bool _isLoading;

    [RelayCommand(CanExecute = nameof(HasSongs))]
    private void ShuffleAndPlay()
    {
        if (Songs.Count == 0) return;
        Random rnd = new();
        List<MediaViewModel> shuffledList = Songs.OrderBy(_ => rnd.Next()).ToList();
        Messenger.Send(new ClearPlaylistMessage());
        Messenger.Send(new QueuePlaylistMessage(shuffledList));
        Messenger.Send(new PlayMediaMessage(shuffledList[0], true));
    }

    [RelayCommand]
    private void SetSortBy(string tag)
    {
        SortBy = tag;
    }
}
