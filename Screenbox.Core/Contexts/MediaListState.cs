#nullable enable

using System.Collections.Generic;
using System.Threading;
using Screenbox.Core.Playback;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Search;

namespace Screenbox.Core.Contexts;

internal sealed class MediaListState
{
    internal Playlist Playlist { get; set; } = new();
    internal List<MediaViewModel> MediaBuffer { get; set; } = new();
    internal IMediaPlayer? MediaPlayer { get; set; }
    internal object? DelayPlay { get; set; }
    internal bool DeferCollectionChanged { get; set; }
    internal StorageFileQueryResult? NeighboringFilesQuery { get; set; }
    internal CancellationTokenSource? PlayFilesCancellation { get; set; }
    internal MediaPlaybackAutoRepeatMode RepeatMode { get; set; }
    internal bool ShuffleMode { get; set; }
    internal MediaViewModel? CurrentItem { get; set; }
    internal int CurrentIndex { get; set; } = -1;
}
