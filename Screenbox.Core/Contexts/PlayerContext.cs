#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Contexts
{
    public sealed class PlayerContext : ObservableObject
    {
        [ObservableProperty]
        private IMediaPlayer? _mediaPlayer;
    }
}
