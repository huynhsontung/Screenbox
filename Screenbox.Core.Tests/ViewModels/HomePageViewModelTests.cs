using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using TUnit.Core;
using Windows.Foundation;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using MediaPlaybackType = Screenbox.Core.Enums.MediaPlaybackType;

namespace Screenbox.Core.Tests.ViewModels;

[NotInParallel("MessengerTests")]
public class HomePageViewModelTests
{
    [Test]
    public async Task PlayCommand_WhenMediaIsActive_SendsTogglePlayPauseMessage()
    {
        var (vm, _, _) = CreateViewModel();
        var media = new MediaViewModel(new UriMediaSource(new Uri("https://example.com/video.mp4")))
        {
            IsMediaActive = true
        };

        TogglePlayPauseMessage? receivedMessage = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<TogglePlayPauseMessage>(recipient, (_, msg) =>
        {
            receivedMessage = msg;
        });

        try
        {
            await vm.PlayCommand.ExecuteAsync(media);
            await Assert.That(receivedMessage).IsNotNull();
            await Assert.That(receivedMessage!.ShowBadge).IsFalse();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Test]
    public async Task PlayCommand_WhenNotFileMediaSource_SendsPlayMediaMessage()
    {
        var (vm, _, _) = CreateViewModel();
        var media = new MediaViewModel(new UriMediaSource(new Uri("https://example.com/video.mp4")))
        {
            IsMediaActive = false
        };

        PlayMediaMessage? receivedMessage = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<PlayMediaMessage>(recipient, (_, msg) =>
        {
            receivedMessage = msg;
        });

        try
        {
            await vm.PlayCommand.ExecuteAsync(media);
            await Assert.That(receivedMessage).IsNotNull();
            await Assert.That(receivedMessage!.Value).IsEqualTo(media);
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Test]
    public async Task PlayCommand_WhenFileMediaSourceWithNullStorageFile_SendsPlayMediaMessage()
    {
        var (vm, _, _) = CreateViewModel();
        var media = new MediaViewModel(new FakeFileMediaSource(null))
        {
            IsMediaActive = false
        };

        PlayMediaMessage? receivedMessage = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<PlayMediaMessage>(recipient, (_, msg) =>
        {
            receivedMessage = msg;
        });

        try
        {
            await vm.PlayCommand.ExecuteAsync(media);
            await Assert.That(receivedMessage).IsNotNull();
            await Assert.That(receivedMessage!.Value).IsEqualTo(media);
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Test]
    public async Task PlayCommand_WhenFileMediaSourceWithStorageFile_RoutesThroughPlayFilesAsync()
    {
        var (vm, playlistService, generalSettings) = CreateViewModel();
        generalSettings.EnqueueAllFilesInFolder = true;

        string tempFilePath = Path.GetTempFileName();
        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(tempFilePath);
            var media = new MediaViewModel(new FakeFileMediaSource(storageFile))
            {
                IsMediaActive = false
            };

            playlistService.PlaylistToReturn = new Playlist(media, new[] { media });

            SetQueueMessage? receivedQueueMessage = null;
            object recipient = new();
            WeakReferenceMessenger.Default.Register<SetQueueMessage>(recipient, (_, msg) =>
            {
                if (ReferenceEquals(msg.Value.CurrentItem, media))
                {
                    receivedQueueMessage = msg;
                }
            });

            try
            {
                await vm.PlayCommand.ExecuteAsync(media);

                await Assert.That(playlistService.CreatedPlaylistRequests.Count).IsEqualTo(1);
                await Assert.That(playlistService.CreatedPlaylistRequests[0].Count).IsEqualTo(1);
                await Assert.That(playlistService.CreatedPlaylistRequests[0][0].Path).IsEqualTo(tempFilePath);
                await Assert.That(receivedQueueMessage).IsNotNull();
            }
            finally
            {
                WeakReferenceMessenger.Default.UnregisterAll(recipient);
            }
        }
        finally
        {
            try
            {
                File.Delete(tempFilePath);
            }
            catch
            {
                // Best-effort temp file cleanup
            }
        }
    }

    [Test]
    public async Task PlayCommand_WhenFolderHasNeighboringFilesAndEnqueueEnabled_SendsEnqueueFolderNotification()
    {
        using var fixture = new TestDirectoryFixture();
        var (vm, playlistService, generalSettings) = CreateViewModel();
        generalSettings.EnqueueAllFilesInFolder = true;

        string tempFilePath = Path.Combine(fixture.DirectoryPath, "video1.mp4");
        File.WriteAllText(tempFilePath, "content");

        var storageFile = await StorageFile.GetFileFromPathAsync(tempFilePath);
        var media1 = new MediaViewModel(new FakeFileMediaSource(storageFile)) { IsMediaActive = false };
        var media2 = new MediaViewModel(new FakeFileMediaSource(storageFile, Path.Combine(fixture.DirectoryPath, "video2.mp4")));

        var folder = await StorageFolder.GetFolderFromPathAsync(fixture.DirectoryPath);
        var query = folder.CreateFileQuery();
        var initialPlaylist = new Playlist(media1, new[] { media1 }) { NeighboringFilesQuery = query };
        playlistService.PlaylistToReturn = initialPlaylist;
        playlistService.UpdatedPlaylistToReturn = new Playlist(media1, new[] { media1, media2 }) { NeighboringFilesQuery = query };

        NotificationMessage? receivedNotification = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<NotificationMessage>(recipient, (_, msg) =>
        {
            if (msg.Kind == NotificationKind.EnqueueFolderItems)
            {
                receivedNotification = msg;
            }
        });

        try
        {
            await vm.PlayCommand.ExecuteAsync(media1);

            await Assert.That(receivedNotification).IsNotNull();
            await Assert.That(receivedNotification!.Kind).IsEqualTo(NotificationKind.EnqueueFolderItems);
            await Assert.That(receivedNotification.Title).IsEqualTo(folder.DisplayName);
            await Assert.That(receivedNotification.ActionCommand).IsNotNull();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Test]
    public async Task PlayCommand_WhenEnqueueFolderActionInvoked_UpdatesQueueWithoutInterruptingPlayback()
    {
        using var fixture = new TestDirectoryFixture();
        var (vm, playlistService, generalSettings) = CreateViewModel();
        generalSettings.EnqueueAllFilesInFolder = true;

        string tempFilePath = Path.Combine(fixture.DirectoryPath, "video1.mp4");
        File.WriteAllText(tempFilePath, "content");

        var storageFile = await StorageFile.GetFileFromPathAsync(tempFilePath);
        var media1 = new MediaViewModel(new FakeFileMediaSource(storageFile)) { IsMediaActive = false };
        var media2 = new MediaViewModel(new FakeFileMediaSource(storageFile, Path.Combine(fixture.DirectoryPath, "video2.mp4")));

        var folder = await StorageFolder.GetFolderFromPathAsync(fixture.DirectoryPath);
        var query = folder.CreateFileQuery();
        var initialPlaylist = new Playlist(media1, new[] { media1 }) { NeighboringFilesQuery = query };
        var expandedPlaylist = new Playlist(media1, new[] { media1, media2 }) { NeighboringFilesQuery = query };
        playlistService.PlaylistToReturn = initialPlaylist;
        playlistService.UpdatedPlaylistToReturn = expandedPlaylist;

        NotificationMessage? receivedNotification = null;
        SetQueueMessage? updatedQueueMessage = null;
        CloseNotificationMessage? closeMessage = null;
        object recipient = new();

        WeakReferenceMessenger.Default.Register<NotificationMessage>(recipient, (_, msg) =>
        {
            if (msg.Kind == NotificationKind.EnqueueFolderItems)
            {
                receivedNotification = msg;
            }
        });
        WeakReferenceMessenger.Default.Register<CloseNotificationMessage>(recipient, (_, msg) =>
        {
            closeMessage = msg;
        });
        WeakReferenceMessenger.Default.Register<QueueRequestMessage>(recipient, (_, msg) =>
        {
            msg.Reply(initialPlaylist);
        });
        WeakReferenceMessenger.Default.Register<SetQueueMessage>(recipient, (_, msg) =>
        {
            if (msg.Value.Items.Count > 1)
            {
                updatedQueueMessage = msg;
            }
        });

        try
        {
            await vm.PlayCommand.ExecuteAsync(media1);

            await Assert.That(receivedNotification).IsNotNull();
            receivedNotification!.ActionCommand!.Execute(null);

            await Assert.That(closeMessage).IsNotNull();
            await Assert.That(updatedQueueMessage).IsNotNull();
            await Assert.That(updatedQueueMessage!.ShouldPlay).IsFalse();
            await Assert.That(updatedQueueMessage.Value.Items.Count).IsEqualTo(2);
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Test]
    public async Task PlayCommand_WhenEnqueueAllFilesInFolderDisabled_DoesNotSendEnqueueNotification()
    {
        using var fixture = new TestDirectoryFixture();
        var (vm, playlistService, generalSettings) = CreateViewModel();
        generalSettings.EnqueueAllFilesInFolder = false;

        string tempFilePath = Path.Combine(fixture.DirectoryPath, "video1.mp4");
        File.WriteAllText(tempFilePath, "content");

        var storageFile = await StorageFile.GetFileFromPathAsync(tempFilePath);
        var media1 = new MediaViewModel(new FakeFileMediaSource(storageFile)) { IsMediaActive = false };
        var media2 = new MediaViewModel(new FakeFileMediaSource(storageFile, Path.Combine(fixture.DirectoryPath, "video2.mp4")));

        var folder = await StorageFolder.GetFolderFromPathAsync(fixture.DirectoryPath);
        var query = folder.CreateFileQuery();
        var initialPlaylist = new Playlist(media1, new[] { media1 }) { NeighboringFilesQuery = query };
        playlistService.PlaylistToReturn = initialPlaylist;
        playlistService.UpdatedPlaylistToReturn = new Playlist(media1, new[] { media1, media2 }) { NeighboringFilesQuery = query };

        NotificationMessage? receivedNotification = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<NotificationMessage>(recipient, (_, msg) =>
        {
            if (msg.Kind == NotificationKind.EnqueueFolderItems)
            {
                receivedNotification = msg;
            }
        });

        try
        {
            await vm.PlayCommand.ExecuteAsync(media1);
            await Assert.That(receivedNotification).IsNull();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Test]
    public async Task PlayCommand_WhenFolderHasSingleFile_DoesNotSendEnqueueNotification()
    {
        using var fixture = new TestDirectoryFixture();
        var (vm, playlistService, generalSettings) = CreateViewModel();
        generalSettings.EnqueueAllFilesInFolder = true;

        string tempFilePath = Path.Combine(fixture.DirectoryPath, "video1.mp4");
        File.WriteAllText(tempFilePath, "content");

        var storageFile = await StorageFile.GetFileFromPathAsync(tempFilePath);
        var media1 = new MediaViewModel(new FakeFileMediaSource(storageFile)) { IsMediaActive = false };

        var folder = await StorageFolder.GetFolderFromPathAsync(fixture.DirectoryPath);
        var query = folder.CreateFileQuery();
        var initialPlaylist = new Playlist(media1, new[] { media1 }) { NeighboringFilesQuery = query };
        playlistService.PlaylistToReturn = initialPlaylist;
        playlistService.UpdatedPlaylistToReturn = initialPlaylist;

        NotificationMessage? receivedNotification = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<NotificationMessage>(recipient, (_, msg) =>
        {
            if (msg.Kind == NotificationKind.EnqueueFolderItems)
            {
                receivedNotification = msg;
            }
        });

        try
        {
            await vm.PlayCommand.ExecuteAsync(media1);
            await Assert.That(receivedNotification).IsNull();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    private static (HomePageViewModel ViewModel, TestPlaylistService PlaylistService, GeneralSettingsContext GeneralSettings) CreateViewModel()
    {
        var recentContext = new RecentContext();
        var selection = new SelectionViewModel();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), new LibraryContext());
        var filesService = new TestFilesService();
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage());
        var playlistService = new TestPlaylistService();
        var logger = NullLogger<HomePageViewModel>.Instance;

        var vm = new HomePageViewModel(
            recentContext,
            selection,
            mediaFactory,
            filesService,
            generalSettings,
            playlistService,
            logger);

        return (vm, playlistService, generalSettings);
    }

    private sealed class FakeFileMediaSource : IFileMediaSource
    {
        private readonly StorageFile? _file;
        public string Location => Path;
        public string Path { get; } = "C:\\test\\video.mp4";
        public string FileType => ".mp4";
        public string Name => "video.mp4";
        public MediaPlaybackType MediaType => MediaPlaybackType.Video;
        public StorageFile? StorageFile => _file;

        public FakeFileMediaSource(StorageFile? file, string? path = null)
        {
            _file = file;
            if (path is not null)
            {
                Path = path;
            }
            else if (file is not null)
            {
                Path = file.Path;
            }
        }

        public Task<StorageFile?> GetStorageFileAsync() => Task.FromResult(_file);
        public void UpdateStorageFile(StorageFile file) { }
    }

    private sealed class TestPlaylistService : IPlaylistService
    {
        public List<IReadOnlyList<IStorageItem>> CreatedPlaylistRequests { get; } = [];
        public Playlist? PlaylistToReturn { get; set; }
        public Playlist? UpdatedPlaylistToReturn { get; set; }

        public Task<Playlist?> TryCreatePlaylistAsync(
            IReadOnlyList<IStorageItem> storageItems,
            StorageFileQueryResult? neighboringFilesQuery = null,
            CancellationToken cancellationToken = default)
        {
            CreatedPlaylistRequests.Add(storageItems);
            return Task.FromResult(PlaylistToReturn);
        }

        public Task<Playlist> AddNeighboringFilesAsync(Playlist playlist, CancellationToken cancellationToken = default) =>
            Task.FromResult(UpdatedPlaylistToReturn ?? playlist);

        public Playlist ShufflePlaylist(Playlist playlist, int? preserveIndex = null) => playlist;
        public Playlist RestoreFromShuffle(Playlist playlist) => playlist;
        public void UpdateShuffleBackup(Playlist playlist, NotifyCollectionChangedEventArgs e) { }
        public IReadOnlyList<int> GetMediaBufferIndices(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode, int bufferSize = 5) => [];
        public Task SavePlaylistAsync(PlaylistRecordDto playlist) => Task.CompletedTask;
        public Task<PlaylistRecordDto?> LoadPlaylistAsync(string id) => Task.FromResult<PlaylistRecordDto?>(null);
        public Task<IReadOnlyList<PlaylistRecordDto>> ListPlaylistsAsync() => Task.FromResult<IReadOnlyList<PlaylistRecordDto>>([]);
        public Task DeletePlaylistAsync(string id) => Task.CompletedTask;
        public Task SaveThumbnailAsync(string mediaLocation, byte[] imageBytes) => Task.CompletedTask;
        public Task<StorageFile?> GetThumbnailFileAsync(string mediaLocation) => Task.FromResult<StorageFile?>(null);
        public Task AddToPlaylistAsync(string playlistId, IReadOnlyList<MediaViewModel> items) => Task.CompletedTask;
        public Task<IReadOnlyList<MediaViewModel>> ImportPlaylistItemsAsync(StorageFile file) => Task.FromResult<IReadOnlyList<MediaViewModel>>([]);
        public Task ExportPlaylistItemsAsync(IReadOnlyList<MediaViewModel> items, StorageFile file) => Task.CompletedTask;
    }

    private sealed class TestFilesService : IFilesService
    {
        public Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file, QueryOptions? options = null) => Task.FromResult<StorageFileQueryResult?>(null);
        public Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery) => Task.FromResult<StorageFile?>(null);
        public Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery) => Task.FromResult<StorageFile?>(null);
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
        public Task<MediaInfo> GetMediaInfoAsync(StorageFile file) => Task.FromResult(new MediaInfo(MediaPlaybackType.Video));
        public IAsyncOperation<StorageFolder> GetFrameCaptureFolderAsync(string token) => throw new NotImplementedException();
    }
}
