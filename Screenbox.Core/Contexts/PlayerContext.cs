#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Contexts;

public sealed partial class PlayerContext : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial IMediaPlayer? MediaPlayer { get; set; }
}
