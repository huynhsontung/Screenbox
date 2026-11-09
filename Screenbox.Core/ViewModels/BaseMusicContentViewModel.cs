using Screenbox.Core.Models;
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
    public partial IReadOnlyList<MediaViewModel> Songs { get; set; } = Array.Empty<MediaViewModel>();

    [ObservableProperty] public partial bool IsLoading { get; set; }

    public IRelayCommand ShuffleAndPlayCommand => field ??= new RelayCommand(ShuffleAndPlay, () => HasSongs);
    private void ShuffleAndPlay()
    {
        if (Songs.Count == 0) return;
        Random rnd = new();
        List<MediaViewModel> shuffledList = Songs.OrderBy(_ => rnd.Next()).ToList();
        var queue = new QueueSession(0, shuffledList)
        {
            ShuffleMode = true,
            ShuffleBackup = new ShuffleBackup(Songs.ToList()),
            Origin = QueueOrigin.ForLibraryQueue(shuffledList[0])
        };
        Messenger.Send(new SetQueueMessage(queue, true));
    }
}
