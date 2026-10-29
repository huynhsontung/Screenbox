using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace Screenbox.Core.Tests.ViewModels;

public class PropertyViewModelTests
{
    private static StorageMediaProbeService CreateStorageProbeService() =>
        new(NullLogger<StorageMediaProbeService>.Instance);

    [Test]
    public async Task LoadPropertiesAsync_ShouldProbeMediaAndPopulateUpdatedProperties()
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
        var probeService = new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Music, probedMusic, new VideoInfo()));
        var media = new MediaViewModel(source)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Music)
        };
        var vm = new PropertyViewModel(new DummyFilesService(), CreateStorageProbeService(), probeService);

        vm.UpdateProperties(media);
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Title).Value).IsEqualTo(string.Empty);

        await vm.LoadPropertiesAsync(media);

        await Assert.That(media.DetailsLoaded).IsTrue();
        await Assert.That(probeService.CallCount).IsEqualTo(1);
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
    public async Task UpdateProperties_WhenCalledMultipleTimes_ShouldUpdatePropertiesWithoutDuplicates()
    {
        var source = new FileMediaSource(@"C:\videos\clip.mp4");
        var media = new MediaViewModel(source)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Video)
        };

        var vm = new PropertyViewModel(new DummyFilesService(), CreateStorageProbeService(), new TestMediaProbeService());

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
        await media.LoadDetailsAsync(new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Video, new MusicInfo { Bitrate = 192000 }, updatedVideo)));
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
            CreateStorageProbeService(),
            new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), vlcVideo)));

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
