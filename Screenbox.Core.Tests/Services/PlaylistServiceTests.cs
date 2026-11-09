using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.Services;

public class PlaylistServiceTests
{
    private sealed class FakeDatabaseService : IDatabaseService
    {
        public readonly Dictionary<string, PlaylistRecordDto> Playlists = new();

        public Task InitializeAsync() => Task.CompletedTask;
        public Task<RawCacheLoadResultDto> LoadLibraryCacheAsync(MediaPlaybackType mediaType) => throw new NotImplementedException();
        public Task SaveMusicCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<MusicCacheRecordDto> records) => throw new NotImplementedException();
        public Task SaveVideoCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<VideoCacheRecordDto> records) => throw new NotImplementedException();
        public Task ReplacePlaybackProgressAsync(IReadOnlyList<MediaPlaybackProgress> snapshot) => throw new NotImplementedException();
        public Task<List<MediaPlaybackProgress>> LoadPlaybackProgressAsync() => throw new NotImplementedException();

        public Task SavePlaylistAsync(PlaylistRecordDto playlist)
        {
            Playlists[playlist.Id] = playlist;
            return Task.CompletedTask;
        }

        public Task<PlaylistRecordDto?> LoadPlaylistAsync(string id)
        {
            Playlists.TryGetValue(id, out var record);
            return Task.FromResult(record);
        }

        public Task<List<PlaylistRecordDto>> ListPlaylistsAsync()
        {
            return Task.FromResult(new List<PlaylistRecordDto>(Playlists.Values));
        }

        public Task DeletePlaylistAsync(string id)
        {
            Playlists.Remove(id);
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task AddToPlaylistAsync_ValidItems_AppendsAndSaves()
    {
        var db = new FakeDatabaseService();
        var playlist = new PlaylistRecordDto
        {
            Id = "p1",
            DisplayName = "My Playlist",
            LastUpdated = DateTimeOffset.UtcNow,
            Items = new List<RawMediaRecordDto>()
        };
        await db.SavePlaylistAsync(playlist);

        var service = new PlaylistService(db, null!);
        var media = new MediaViewModel(new Screenbox.Core.MediaSources.UriMediaSource(new Uri("https://example.com/test.mp3")))
        {
            Name = "Test Song"
        };

        await service.AddToPlaylistAsync("p1", new[] { media });

        var updated = await service.LoadPlaylistAsync("p1");
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Items.Count).IsEqualTo(1);
        await Assert.That(updated.Items[0].Title).IsEqualTo("Test Song");
    }

    [Test]
    public async Task AddToPlaylistAsync_NonExistentPlaylist_ThrowsInvalidOperationException()
    {
        var db = new FakeDatabaseService();
        var service = new PlaylistService(db, null!);
        var media = new MediaViewModel(new Screenbox.Core.MediaSources.UriMediaSource(new Uri("https://example.com/test.mp3")));

        await Assert.That(async () => await service.AddToPlaylistAsync("non-existent", new[] { media }))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DeletePlaylistAsync_RemovesFromDatabase()
    {
        var db = new FakeDatabaseService();
        var playlist = new PlaylistRecordDto
        {
            Id = "p1",
            DisplayName = "My Playlist",
            LastUpdated = DateTimeOffset.UtcNow,
            Items = new List<RawMediaRecordDto>()
        };
        await db.SavePlaylistAsync(playlist);

        var service = new PlaylistService(db, null!);
        await service.DeletePlaylistAsync("p1");

        var loaded = await service.LoadPlaylistAsync("p1");
        await Assert.That(loaded).IsNull();
    }
}
