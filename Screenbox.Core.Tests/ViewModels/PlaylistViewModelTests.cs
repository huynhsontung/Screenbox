using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class PlaylistViewModelTests
{
    private sealed class FakePlaylistThumbnailService : IPlaylistThumbnailService
    {
        public Func<string, CancellationToken, Task<byte[]?>>? OnRead { get; set; }
        public Func<string, IReadOnlyList<MediaViewModel>, CancellationToken, Task<byte[]?>>? OnBuild { get; set; }
        public int ReadCallCount { get; private set; }
        public int BuildCallCount { get; private set; }
        public List<string> DeletedIds { get; } = new();

        public Task<byte[]?> ReadThumbnailAsync(string playlistId, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return OnRead is not null ? OnRead(playlistId, cancellationToken) : Task.FromResult<byte[]?>(null);
        }

        public Task<byte[]?> BuildAndCacheThumbnailAsync(
            string playlistId,
            IReadOnlyList<MediaViewModel> items,
            CancellationToken cancellationToken = default)
        {
            BuildCallCount++;
            return OnBuild is not null ? OnBuild(playlistId, items, cancellationToken) : Task.FromResult<byte[]?>(null);
        }

        public Task DeleteThumbnailAsync(string playlistId)
        {
            DeletedIds.Add(playlistId);
            return Task.CompletedTask;
        }
    }

    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private static PlaylistViewModel CreatePlaylist(IPlaylistThumbnailService? thumbnailService = null) =>
        new(null!, thumbnailService ?? new FakePlaylistThumbnailService(), null!, NullLogger<PlaylistViewModel>.Instance, new WeakReferenceMessenger())
        {
            Name = "Favorites"
        };

    [Test]
    public async Task AddItemsAsync_DuplicateMedia_WrapsCanonicalMediaInDistinctEntries()
    {
        var playlist = CreatePlaylist();
        var media = CreateDummyMedia("song1");

        await playlist.AddItemsAsync(new[] { media, media });

        await Assert.That(playlist.Items.Count).IsEqualTo(2);
        await Assert.That(playlist.Items[0]).IsNotSameReferenceAs(playlist.Items[1]);
        await Assert.That(playlist.Items[0].Media).IsSameReferenceAs(media);
        await Assert.That(playlist.Items[1].Media).IsSameReferenceAs(media);
    }

    [Test]
    public async Task AddItemsAtIndexAsync_InsertsDuplicateAtTargetIndexWithoutCloningMedia()
    {
        var playlist = CreatePlaylist();
        var media1 = CreateDummyMedia("song1");
        var media2 = CreateDummyMedia("song2");

        await playlist.AddItemsAsync(new[] { media1, media2 });
        await playlist.AddItemsAtIndexAsync(new[] { media1 }, insertIndex: 1);

        await Assert.That(playlist.Items.Count).IsEqualTo(3);
        await Assert.That(playlist.Items[0].Media).IsSameReferenceAs(media1);
        await Assert.That(playlist.Items[1].Media).IsSameReferenceAs(media1);
        await Assert.That(playlist.Items[2].Media).IsSameReferenceAs(media2);
        await Assert.That(playlist.Items[0]).IsNotSameReferenceAs(playlist.Items[1]);
    }

    [Test]
    public async Task Remove_DuplicateEntry_RemovesOnlyTargetedEntry()
    {
        var playlist = CreatePlaylist();
        var media = CreateDummyMedia("song1");

        await playlist.AddItemsAsync(new[] { media, media });
        var firstEntry = playlist.Items[0];
        var secondEntry = playlist.Items[1];

        playlist.Items.Remove(firstEntry);

        await Assert.That(playlist.Items.Count).IsEqualTo(1);
        await Assert.That(playlist.Items[0]).IsSameReferenceAs(secondEntry);
        await Assert.That(playlist.Items[0].Media).IsSameReferenceAs(media);
    }

    [Test]
    public async Task ToQueueSession_WrapsCanonicalMediaInQueueItemsWithPlaylistOrigin()
    {
        var playlist = CreatePlaylist();
        var media1 = CreateDummyMedia("song1");
        var media2 = CreateDummyMedia("song2");

        await playlist.AddItemsAsync(new[] { media1, media2, media1 });
        var session = playlist.ToQueueSession();

        await Assert.That(session.Items.Count).IsEqualTo(3);
        await Assert.That(session.CurrentIndex).IsEqualTo(0);
        await Assert.That(session.CurrentItem?.Origin?.Kind).IsEqualTo(RecentItemKind.Playlist);
        await Assert.That(session.CurrentItem?.Origin?.SourceId).IsEqualTo(playlist.Id);
        await Assert.That(session.Items[0].Media).IsSameReferenceAs(media1);
        await Assert.That(session.Items[1].Media).IsSameReferenceAs(media2);
        await Assert.That(session.Items[2].Media).IsSameReferenceAs(media1);
        await Assert.That(session.Items[0]).IsNotSameReferenceAs(session.Items[2]);
    }

    [Test]
    public async Task PlayCommand_DuplicateEntryClicked_StartsQueueAtClickedIndex()
    {
        var messenger = new WeakReferenceMessenger();
        var playlist = new PlaylistViewModel(null!, new FakePlaylistThumbnailService(), null!, NullLogger<PlaylistViewModel>.Instance, messenger)
        {
            Name = "Favorites"
        };
        var mediaA = CreateDummyMedia("songA");
        var mediaB = CreateDummyMedia("songB");
        await playlist.AddItemsAsync(new[] { mediaA, mediaB, mediaA });

        var detailsVm = new PlaylistDetailsPageViewModel(null!, null!, null!, null!, null!, messenger);
        detailsVm.OnNavigatedTo(playlist);

        SetQueueMessage? sentMessage = null;
        var recipient = new object();
        messenger.Register<object, SetQueueMessage>(recipient, (_, msg) => sentMessage = msg);

        detailsVm.PlayCommand.Execute(playlist.Items[2]);

        await Assert.That(sentMessage).IsNotNull();
        await Assert.That(sentMessage!.ShouldPlay).IsTrue();
        await Assert.That(sentMessage.Value.CurrentIndex).IsEqualTo(2);
        await Assert.That(sentMessage.Value.CurrentItem?.Media).IsSameReferenceAs(mediaA);
        await Assert.That(sentMessage.Value.Items.Count).IsEqualTo(3);
        await Assert.That(sentMessage.Value.CurrentItem?.Origin?.Kind).IsEqualTo(RecentItemKind.Playlist);
        await Assert.That(sentMessage.Value.CurrentItem?.Origin?.SourceId).IsEqualTo(playlist.Id);
    }

    [Test]
    public async Task LoadThumbnailAsync_PlaceholderPlaylist_ReadsCacheWithoutGeneratingCollage()
    {
        var thumbnails = new FakePlaylistThumbnailService();
        var playlist = CreatePlaylist(thumbnails);
        string placeholderId = Guid.NewGuid().ToString();
        playlist.LoadPlaceholder(placeholderId, "Recent Playlist");

        await playlist.LoadThumbnailAsync();

        await Assert.That(thumbnails.ReadCallCount).IsEqualTo(1);
        await Assert.That(thumbnails.BuildCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task LoadThumbnailAsync_ConcurrentCalls_DeduplicatesInFlightRead()
    {
        var readGate = new TaskCompletionSource<byte[]?>();
        var thumbnails = new FakePlaylistThumbnailService
        {
            OnRead = (_, _) => readGate.Task
        };
        var playlist = CreatePlaylist(thumbnails);
        playlist.LoadPlaceholder(Guid.NewGuid().ToString(), "Recent Playlist");

        Task first = playlist.LoadThumbnailAsync();
        Task second = playlist.LoadThumbnailAsync();
        readGate.SetResult(null);

        await Task.WhenAll(first, second);

        await Assert.That(thumbnails.ReadCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task UpdateThumbnailCollageAsync_EmptyPlaylist_DeletesCachedThumbnail()
    {
        var thumbnails = new FakePlaylistThumbnailService();
        var playlist = CreatePlaylist(thumbnails);

        await playlist.UpdateThumbnailCollageAsync();

        await Assert.That(playlist.Thumbnail).IsNull();
        await Assert.That(thumbnails.DeletedIds.Count).IsEqualTo(1);
        await Assert.That(thumbnails.DeletedIds[0]).IsEqualTo(playlist.Id);
    }

    [Test]
    public async Task UpdateThumbnailCollageAsync_SupersededUpdate_CancelsPreviousOperation()
    {
        var firstStarted = new TaskCompletionSource();
        var firstCanceled = new TaskCompletionSource<bool>();
        int callIndex = 0;

        var thumbnails = new FakePlaylistThumbnailService
        {
            OnBuild = async (_, _, ct) =>
            {
                int current = Interlocked.Increment(ref callIndex);
                if (current == 1)
                {
                    firstStarted.TrySetResult();
                    try
                    {
                        await Task.Delay(Timeout.Infinite, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        firstCanceled.TrySetResult(true);
                        throw;
                    }
                }

                return null;
            }
        };

        var playlist = CreatePlaylist(thumbnails);
        playlist.Items.Add(new PlaylistItemViewModel(CreateDummyMedia("song1")));

        Task firstUpdate = playlist.UpdateThumbnailCollageAsync();
        await firstStarted.Task;

        Task secondUpdate = playlist.UpdateThumbnailCollageAsync();
        await Task.WhenAll(firstUpdate, secondUpdate);

        await Assert.That(await firstCanceled.Task).IsTrue();
        await Assert.That(thumbnails.BuildCallCount).IsEqualTo(2);
    }

    [Test]
    public async Task ShouldRefreshThumbnail_ChecksFullScanWindow()
    {
        var playlist = CreatePlaylist();
        for (int i = 0; i < 35; i++)
        {
            playlist.Items.Add(new PlaylistItemViewModel(CreateDummyMedia($"song{i}")));
        }

        var item = playlist.Items[0];

        await Assert.That(playlist.ShouldRefreshThumbnail(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index: 4))).IsTrue();
        await Assert.That(playlist.ShouldRefreshThumbnail(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index: 31))).IsTrue();
        await Assert.That(playlist.ShouldRefreshThumbnail(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index: 32))).IsFalse();

        await Assert.That(playlist.ShouldRefreshThumbnail(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item, index: 31))).IsTrue();
        await Assert.That(playlist.ShouldRefreshThumbnail(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item, index: 32))).IsFalse();

        await Assert.That(playlist.ShouldRefreshThumbnail(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, index: 32, oldIndex: 31))).IsTrue();
        await Assert.That(playlist.ShouldRefreshThumbnail(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, index: 33, oldIndex: 32))).IsFalse();
    }

    [Test]
    public async Task LoadThumbnailAsync_SupersededWhileReadingCache_DoesNotDeleteCachedThumbnail()
    {
        var readStarted = new TaskCompletionSource();
        var readGate = new TaskCompletionSource<byte[]?>();
        var thumbnails = new FakePlaylistThumbnailService
        {
            OnRead = (_, _) =>
            {
                readStarted.TrySetResult();
                return readGate.Task;
            }
        };

        var playlist = CreatePlaylist(thumbnails);
        playlist.Items.Add(new PlaylistItemViewModel(CreateDummyMedia("song1")));

        Task loadTask = playlist.LoadThumbnailAsync();
        await readStarted.Task;

        Task updateTask = playlist.UpdateThumbnailCollageAsync();
        readGate.SetResult(new byte[] { 1, 2, 3 });

        await Task.WhenAll(loadTask, updateTask);

        await Assert.That(thumbnails.DeletedIds.Count).IsEqualTo(0);
    }
}
