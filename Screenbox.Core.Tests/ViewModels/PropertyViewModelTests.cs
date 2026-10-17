using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
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
    [Test]
    public async Task LoadPropertiesAsync_ShouldProbeMediaAndPopulateUpdatedProperties()
    {
        var uri = new Uri("file:///c:/music/song.ogg");
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
        var media = new MediaViewModel(new PlayerContext(), new TestPlayerService(), probeService, uri)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Music)
        };
        var vm = new PropertyViewModel(new DummyFilesService());

        vm.UpdateProperties(media);
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Title).Value).IsEqualTo(string.Empty);

        await vm.LoadPropertiesAsync(media);

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
    public async Task UpdateProperties_WhenCalledMultipleTimes_ShouldUpdateCollectionsInPlaceWithoutDuplicates()
    {
        var uri = new Uri("file:///c:/videos/clip.mp4");
        var media = new MediaViewModel(new PlayerContext(), new TestPlayerService(), new TestMediaProbeService(), uri)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Video, "Initial Title")
        };

        var vm = new PropertyViewModel(new DummyFilesService());

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
        media.UpdateParsedMediaInfo(new MediaInfo(MediaPlaybackType.Video, new MusicInfo { Bitrate = 192000 }, updatedVideo));
        vm.UpdateProperties(media);

        await Assert.That(vm.MediaProperties.Count).IsEqualTo(initialMediaCount);
        await Assert.That(vm.VideoProperties.Count).IsEqualTo(initialVideoCount);
        await Assert.That(vm.AudioProperties.Count).IsEqualTo(initialAudioCount);
        await Assert.That(vm.MediaProperties.First(p => p.Key == MediaProperty.Title).Value).IsEqualTo("Updated Title");
        await Assert.That(vm.VideoProperties.First(p => p.Key == MediaProperty.Resolution).Value).IsEqualTo("1920×1080");
        await Assert.That(vm.VideoProperties.First(p => p.Key == MediaProperty.BitRate).Value).IsEqualTo("8000 kbps");
        await Assert.That(vm.AudioProperties.First(p => p.Key == MediaProperty.BitRate).Value).IsEqualTo("192 kbps");
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
