#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;

namespace Screenbox.Core.ViewModels;
public abstract partial class BaseMusicContentViewModel : ObservableRecipient
{
    private bool HasSongs => Songs.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShuffleAndPlayCommand))]
    private IReadOnlyList<MediaViewModel> _songs = Array.Empty<MediaViewModel>();

    [ObservableProperty] private bool _isLoading;

    [RelayCommand(CanExecute = nameof(HasSongs))]
    private void ShuffleAndPlay()
    {
        if (Songs.Count == 0) return;
        Random rnd = new();
        List<MediaViewModel> shuffledList = Songs.OrderBy(_ => rnd.Next()).ToList();
        var playlist = new Models.Playlist(0, shuffledList);
        Messenger.Send(new QueuePlaylistMessage(playlist, true));
    }
}
