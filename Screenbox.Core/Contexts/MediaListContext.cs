#nullable enable

using System.Collections.Generic;
using System.Threading;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Storage.Search;

namespace Screenbox.Core.Contexts;

public sealed class MediaListContext
{
    public Playlist Playlist { get; set; } = new();
    public List<MediaViewModel> MediaBuffer { get; set; } = new();
    public IMediaPlayer? MediaPlayer { get; set; }
    public object? DelayPlay { get; set; }
    public bool DeferCollectionChanged { get; set; }
    public StorageFileQueryResult? NeighboringFilesQuery { get; set; }
    public CancellationTokenSource? PlayFilesCancellation { get; set; }
    public MediaPlaybackAutoRepeatMode RepeatMode { get; set; }
    public bool ShuffleMode { get; set; }
    public MediaViewModel? CurrentItem { get; set; }
    public int CurrentIndex { get; set; } = -1;
}
