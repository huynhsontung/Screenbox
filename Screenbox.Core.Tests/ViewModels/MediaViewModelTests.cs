using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace Screenbox.Core.Tests.ViewModels;

public class MediaViewModelTests
{
    [Test]
    public async Task UpdateParsedMediaInfo_ShouldMergePropertiesAndUpdateCaptions()
    {
        var vm = new MediaViewModel(
            new PlayerContext(),
            new TestPlayerService(),
            new TestMediaProbeService(),
            new Uri("file:///c:/music/track01.flac"));
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
            {
                changedProperties.Add(e.PropertyName);
            }
        };

        var parsedMusic = new MusicInfo
        {
            Title = "Parsed Song",
            Artist = "Parsed Artist",
            Album = "Parsed Album",
            TrackNumber = 3,
            Year = 2024,
            Duration = TimeSpan.FromMinutes(4),
            Bitrate = 1000000
        };
        var parsedInfo = new MediaInfo(MediaPlaybackType.Music, parsedMusic, new VideoInfo());

        vm.UpdateParsedMediaInfo(parsedInfo);

        await Assert.That(vm.IsMediaInfoParsed).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Music);
        await Assert.That(vm.Name).IsEqualTo("Parsed Song");
        await Assert.That(vm.Caption).IsEqualTo("Parsed Artist");
        await Assert.That(vm.AltCaption).IsEqualTo("Parsed Artist – Parsed Album");
        await Assert.That(vm.Duration).IsEqualTo(TimeSpan.FromMinutes(4));
        await Assert.That(vm.TrackNumberText).IsEqualTo("3");
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.MediaInfo));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.Duration));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.MediaType));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.TrackNumberText));
    }

    [Test]
    public async Task LoadDetailsAsync_WhenItemIsCreated_ShouldOpportunisticallyProbeMediaInfo()
    {
        var uri = new Uri("http://example.com/stream.mp3");
        var playerService = new TestPlayerService();
        var parsedMusic = new MusicInfo
        {
            Title = "Live Stream Track",
            Artist = "Stream Artist",
            Album = "Stream Album",
            Duration = TimeSpan.FromSeconds(210),
            Bitrate = 256000
        };
        var parsedInfo = new MediaInfo(MediaPlaybackType.Music, parsedMusic, new VideoInfo());
        var probeService = new TestMediaProbeService(parsedInfo);
        var vm = new MediaViewModel(new PlayerContext(), playerService, probeService, uri);

        // When Item is not yet created (e.g. during fast indexing), LoadDetailsAsync should not probe
        await vm.LoadDetailsAsync(new DummyFilesService());
        await Assert.That(probeService.CallCount).IsEqualTo(0);
        await Assert.That(vm.IsMediaInfoParsed).IsFalse();

        // Once Item is created (e.g. when playback starts), LoadDetailsAsync opportunistically probes
        var playbackItem = playerService.CreatePlaybackItem(null!, uri);
        vm.Item = new Lazy<PlaybackItem?>(playbackItem);
        _ = vm.Item.Value;

        await vm.LoadDetailsAsync(new DummyFilesService());

        await Assert.That(probeService.CallCount).IsEqualTo(1);
        await Assert.That(vm.IsMediaInfoParsed).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Music);
        await Assert.That(vm.Name).IsEqualTo("Live Stream Track");
        await Assert.That(vm.Caption).IsEqualTo("Stream Artist");
        await Assert.That(vm.AltCaption).IsEqualTo("Stream Artist – Stream Album");
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(256000U);
    }

    [Test]
    public async Task ParseDetailsAsync_WhenNotYetParsed_ShouldInvokeProbeServiceAndMergeProperties()
    {
        var uri = new Uri("file:///c:/videos/movie.mkv");
        var probedVideo = new VideoInfo
        {
            Title = "Probed Movie",
            Subtitle = "Director's Cut",
            Year = 2025,
            Duration = TimeSpan.FromMinutes(120),
            Width = 3840,
            Height = 2160,
            Bitrate = 15000000
        };
        var probedMusic = new MusicInfo
        {
            Bitrate = 448000
        };
        var probeService = new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Video, probedMusic, probedVideo));
        var vm = new MediaViewModel(new PlayerContext(), new TestPlayerService(), probeService, uri);

        await vm.ParseDetailsAsync();

        await Assert.That(probeService.CallCount).IsEqualTo(1);
        await Assert.That(vm.IsMediaInfoParsed).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Video);
        await Assert.That(vm.Name).IsEqualTo("Probed Movie");
        await Assert.That(vm.MediaInfo.VideoProperties.Width).IsEqualTo(3840U);
        await Assert.That(vm.MediaInfo.VideoProperties.Height).IsEqualTo(2160U);
        await Assert.That(vm.MediaInfo.VideoProperties.Bitrate).IsEqualTo(15000000U);
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(448000U);

        await vm.ParseDetailsAsync();
        await Assert.That(probeService.CallCount).IsEqualTo(1);
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
        public Task<MediaInfo> GetMediaInfoAsync(StorageFile file) => Task.FromResult(new MediaInfo(MediaPlaybackType.Unknown));
        public IAsyncOperation<StorageFolder> GetFrameCaptureFolderAsync(string token) => throw new NotImplementedException();
    }
}
