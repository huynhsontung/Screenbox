using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Helpers;

namespace Screenbox.Core.ViewModels;

public abstract partial class BaseMusicContentViewModel : ObservableRecipient
{
    private bool HasSongs => Songs.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShuffleAndPlayCommand))]
    public partial IReadOnlyList<MediaViewModel> Songs { get; set; } = Array.Empty<MediaViewModel>();

    [ObservableProperty] public partial bool IsLoading { get; set; }

    public IRelayCommand ShuffleAndPlayCommand => field ??= new RelayCommand(ShuffleAndPlay, () => HasSongs);
    private void ShuffleAndPlay()
    {
        if (Songs.Count == 0) return;
        Messenger.SendShuffleAndPlay(Songs);
    }
}
