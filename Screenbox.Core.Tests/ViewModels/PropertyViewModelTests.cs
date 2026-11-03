using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using AudioTrack = Screenbox.Core.Playback.AudioTrack;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;
using MediaPlaybackType = Screenbox.Core.Enums.MediaPlaybackType;
using VideoTrack = Screenbox.Core.Playback.VideoTrack;

namespace Screenbox.Core.Tests.ViewModels;

public partial class PropertyViewModelTests
{
    private static StorageMetadataService CreateStorageMetadataService() =>
        new(NullLogger<StorageMetadataService>.Instance);

    private static PlayerContext CreatePlayerContextWithPlayer() =>
        new() { MediaPlayer = new TestMediaPlayer() };

    private sealed partial class DummyPlaybackItem : IPlaybackItem
    {
        public bool IsDisposed { get; private set; }
        public IMediaSource Source { get; init; } = null!;
        public TimeSpan? Duration => null;
        public ISingleSelectMediaTrackList<AudioTrack> AudioTracks => throw new NotImplementedException();
        public ISingleSelectMediaTrackList<VideoTrack> VideoTracks => throw new NotImplementedException();
        public ISubtitleTrackList SubtitleTracks => throw new NotImplementedException();
        public IReadOnlyList<ChapterCue> Chapters => Array.Empty<ChapterCue>();
        public Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExpandedMediaItem>>(Array.Empty<ExpandedMediaItem>());
        public void Dispose() => IsDisposed = true;
    }

    private sealed class FakePlayerService : IPlayerService
    {
        public int CreateCount { get; private set; }
        public string[]? LastOptions { get; private set; }
        public DummyPlaybackItem? LastCreatedItem { get; private set; }

        public IMediaPlayer Initialize(string[] swapChainOptions) => throw new NotImplementedException();

        public IPlaybackItem CreatePlaybackItem(IMediaPlayer player, IMediaSource source, params string[] options)
        {
            CreateCount++;
            LastOptions = options;
            LastCreatedItem = new DummyPlaybackItem { Source = source };
            return LastCreatedItem;
        }

        public void DisposePlayer(IMediaPlayer player) { }
    }

    private sealed partial class FakeBufferManager : IPlayQueueBufferManager
    {
        private readonly Dictionary<MediaViewModel, IPlaybackItem> _items = new();

        public IReadOnlyList<MediaViewModel> BufferedItems => Array.Empty<MediaViewModel>();

        public void AddBufferedItem(MediaViewModel vm, IPlaybackItem item) => _items[vm] = item;

        public bool TryGetPlaybackItem(MediaViewModel vm, [NotNullWhen(true)] out IPlaybackItem? item) =>
            _items.TryGetValue(vm, out item);

        public IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, MediaViewModel vm) => null;
        public void CleanPlaybackItem(MediaViewModel vm) { }
        public void Clear() { }
        public void UpdateBuffer(IMediaPlayer? player, Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode) { }
        public void Dispose() { }
    }

    [Test]
    public async Task LoadPropertiesAsync_ShouldExtractMetadataAndPopulateUpdatedProperties()
    {
        var source = new FileMediaSource(@"C:\music\song.ogg");
        var probedMusic = new MusicInfo
        {
            Title = "Probed Ogg Title",
            Artist = "Probed Artist",
            Album = "Probed Album",
            AlbumArtist = "Probed Album Artist",
            Genre = "Electronic",
            TrackNumber = 7,
            Year = 2024,
            Duration = TimeSpan.FromSeconds(195),
            Bitrate = 256000
        };
        var playbackMetadataService = new TestMetadataService(new MediaInfo(MediaPlaybackType.Music, probedMusic, new VideoInfo()));
        var playerService = new FakePlayerService();
        var media = new MediaViewModel(source)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Music)
        };
        media.SetOptions(":network-caching=1000");

        var vm = new PropertyViewModel(
            new DummyFilesService(),
            CreateStorageMetadataService(),
            playbackMetadataService,
            CreatePlayerContextWithPlayer(),
            playerService,
            new FakeBufferManager());

        vm.UpdateProperties(media);
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Title).Value).IsEqualTo(string.Empty);

        await vm.LoadPropertiesAsync(media);

        await Assert.That(media.DetailsLoaded).IsTrue();
        await Assert.That(playbackMetadataService.CallCount).IsEqualTo(1);
        await Assert.That(playerService.CreateCount).IsEqualTo(1);
        await Assert.That(playerService.LastOptions).IsNotNull();
        await Assert.That(playerService.LastOptions!).Contains(":network-caching=1000");
        await Assert.That(playerService.LastCreatedItem!.IsDisposed).IsTrue();
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Title).Value).IsEqualTo("Probed Ogg Title");
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.ContributingArtists).Value).IsEqualTo("Probed Artist");
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Album).Value).IsEqualTo("Probed Album");
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.AlbumArtist).Value).IsEqualTo("Probed Album Artist");
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Genre).Value).IsEqualTo("Electronic");
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Track).Value).IsEqualTo("7");
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Year).Value).IsEqualTo("2024");
        await Assert.That(vm.AudioProperties.First(p => p.Key == MediaProperty.BitRate).Value).IsEqualTo("256 kbps");
    }

    [Test]
    public async Task LoadPropertiesAsync_WhenItemIsBuffered_UsesBufferedItemWithoutCreatingTempItem()
    {
        var source = new FileMediaSource(@"C:\videos\movie.mp4");
        var media = new MediaViewModel(source)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Video)
        };
        var bufferedItem = new DummyPlaybackItem { Source = source };
        var bufferManager = new FakeBufferManager();
        bufferManager.AddBufferedItem(media, bufferedItem);

        var playerService = new FakePlayerService();
        var playbackMetadataService = new TestMetadataService(new MediaInfo(
            MediaPlaybackType.Video,
            new MusicInfo(),
            new VideoInfo { Width = 1920, Height = 1080 }));

        var vm = new PropertyViewModel(
            new DummyFilesService(),
            CreateStorageMetadataService(),
            playbackMetadataService,
            CreatePlayerContextWithPlayer(),
            playerService,
            bufferManager);

        await vm.LoadPropertiesAsync(media);

        await Assert.That(playerService.CreateCount).IsEqualTo(0);
        await Assert.That(playbackMetadataService.LastItem).IsSameReferenceAs(bufferedItem);
        await Assert.That(bufferedItem.IsDisposed).IsFalse();
        await Assert.That(vm.VideoProperties.First(p => p.Key == MediaProperty.Resolution).Value).IsEqualTo("1920×1080");
    }

    [Test]
    public async Task UpdateProperties_WhenCalledMultipleTimes_ShouldUpdatePropertiesWithoutDuplicates()
    {
        var source = new FileMediaSource(@"C:\videos\clip.mp4");
        var media = new MediaViewModel(source)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Video)
        };

        var vm = new PropertyViewModel(
            new DummyFilesService(),
            CreateStorageMetadataService(),
            new TestMetadataService(),
            CreatePlayerContextWithPlayer(),
            new FakePlayerService(),
            new FakeBufferManager());

        vm.UpdateProperties(media);
        int initialMediaCount = vm.MediaProperties.Count;
        int initialVideoCount = vm.VideoProperties.Count;
        int initialAudioCount = vm.AudioProperties.Count;

        var updatedVideo = new VideoInfo
        {
            Title = "Updated Title",
            Width = 1920,
            Height = 1080,
            Bitrate = 8000000
        };
        await media.LoadDetailsAsync((ISourceMetadataService)new TestMetadataService(new MediaInfo(MediaPlaybackType.Video, new MusicInfo { Bitrate = 192000 }, updatedVideo)));
        vm.UpdateProperties(media);

        await Assert.That(vm.MediaProperties.Count).IsEqualTo(initialMediaCount);
        await Assert.That(vm.VideoProperties.Count).IsEqualTo(initialVideoCount);
        await Assert.That(vm.AudioProperties.Count).IsEqualTo(initialAudioCount);
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Title).Value).IsEqualTo("Updated Title");
        await Assert.That(vm.VideoProperties.First(p => p.Key == MediaProperty.Resolution).Value).IsEqualTo("1920×1080");
        await Assert.That(vm.VideoProperties.First(p => p.Key == MediaProperty.BitRate).Value).IsEqualTo("8000 kbps");
        await Assert.That(vm.AudioProperties.First(p => p.Key == MediaProperty.BitRate).Value).IsEqualTo("192 kbps");
    }

    [Test]
    public async Task LoadPropertiesAsync_WhenMediaAlreadyHasStorageProperties_ShouldNotResetPropertiesToEmpty()
    {
        var source = new FileMediaSource(@"C:\videos\movie.mp4");
        var videoInfo = new VideoInfo
        {
            Title = "Storage Title",
            Bitrate = 5000000,
            Producers = "Storage Producer"
        };
        var media = new MediaViewModel(source)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), videoInfo)
        };

        var vlcVideo = new VideoInfo
        {
            Width = 1920,
            Height = 1080,
            Bitrate = 0
        };
        var vm = new PropertyViewModel(
            new DummyFilesService(),
            CreateStorageMetadataService(),
            new TestMetadataService(new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), vlcVideo)),
            CreatePlayerContextWithPlayer(),
            new FakePlayerService(),
            new FakeBufferManager());

        await vm.LoadPropertiesAsync(media);

        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Title).Value).IsEqualTo("Storage Title");
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Producers).Value).IsEqualTo("Storage Producer");
        await Assert.That(vm.VideoProperties.First(p => p.Key == MediaProperty.BitRate).Value).IsEqualTo("5000 kbps");
        await Assert.That(vm.VideoProperties.First(p => p.Key == MediaProperty.Resolution).Value).IsEqualTo("1920×1080");
    }

    private sealed class DummyFilesService : IFilesService
    {
        public Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file, QueryOptions? options = null) => throw new NotImplementedException();
        public Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery) => throw new NotImplementedException();
        public Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery) => throw new NotImplementedException();
        public StorageItemQueryResult GetSupportedItems(StorageFolder folder) => throw new NotImplementedException();
        public IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder) => throw new NotImplementedException();
        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats) => throw new NotImplementedException();
        public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(params string[] formats) => throw new NotImplementedException();
        public IAsyncOperation<StorageFile> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypes, PickerLocationId startLocation = PickerLocationId.ComputerFolder) => throw new NotImplementedException();
        public IAsyncOperation<StorageFolder> PickFolderAsync() => throw new NotImplementedException();
        public Task OpenFileLocationAsync(string path) => Task.CompletedTask;
        public Task OpenFileLocationAsync(StorageFile file) => Task.CompletedTask;
        public void AddToRecent(IStorageItem item) { }
        public Task AddToRecentAsync(IMediaSource? source, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IAsyncOperation<StorageFolder> GetFrameCaptureFolderAsync(string token) => throw new NotImplementedException();
    }
}
