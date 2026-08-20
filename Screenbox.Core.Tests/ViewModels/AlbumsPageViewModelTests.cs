using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class AlbumsPageViewModelTests
{
    [Test]
    public async Task Constructor_ShouldInitializeSortByFromSettings()
    {
        var settings = new TestSettingsService { PersistentAlbumsSortOrder = AlbumSortOrder.Year };
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        await Assert.That(vm.SortBy).IsEqualTo(AlbumSortOrder.Year);
        await Assert.That(vm.SelectedGenre).IsNull();
    }

    [Test]
    public async Task SortBy_WhenChanged_ShouldUpdatePersistentSettings()
    {
        var settings = new TestSettingsService { PersistentAlbumsSortOrder = AlbumSortOrder.Title };
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        vm.SortBy = AlbumSortOrder.Artist;

        await Assert.That(settings.PersistentAlbumsSortOrder).IsEqualTo(AlbumSortOrder.Artist);
    }

    [Test]
    public async Task SetSortByCommand_WhenExecuted_ShouldUpdateSortByAndSettings()
    {
        var settings = new TestSettingsService { PersistentAlbumsSortOrder = AlbumSortOrder.Title };
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        vm.SetSortByCommand.Execute(AlbumSortOrder.DateAdded);

        await Assert.That(vm.SortBy).IsEqualTo(AlbumSortOrder.DateAdded);
        await Assert.That(settings.PersistentAlbumsSortOrder).IsEqualTo(AlbumSortOrder.DateAdded);
    }

    [Test]
    public async Task SetGenreCommand_WhenExecuted_ShouldUpdateSelectedGenre()
    {
        var settings = new TestSettingsService();
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        vm.SetGenreCommand.Execute("Jazz");
        await Assert.That(vm.SelectedGenre).IsEqualTo("Jazz");

        vm.SetGenreCommand.Execute(string.Empty);
        await Assert.That(vm.SelectedGenre).IsEqualTo(string.Empty);

        vm.SetGenreCommand.Execute(null);
        await Assert.That(vm.SelectedGenre).IsNull();
    }

    [Test]
    public async Task FetchAlbums_WhenFilteredByGenre_ShouldFilterAlbumsAndSongs()
    {
        var song1 = CreateSong("Song 1", "Rock");
        var album1 = new AlbumViewModel("Album Rock", "Artist 1");
        album1.RelatedSongs.Add(song1);

        var song2 = CreateSong("Song 2", "Jazz");
        var album2 = new AlbumViewModel("Album Jazz", "Artist 2");
        album2.RelatedSongs.Add(song2);

        var song3 = CreateSong("Song 3", "");
        var album3 = new AlbumViewModel("Album Unknown", "Artist 3");
        album3.RelatedSongs.Add(song3);

        var songs = new List<MediaViewModel> { song1, song2, song3 };
        var albums = new Dictionary<string, AlbumViewModel>
        {
            ["Album Rock"] = album1,
            ["Album Jazz"] = album2,
            ["Album Unknown"] = album3
        };

        var musicLibrary = new MusicLibrary(
            songs,
            albums,
            new Dictionary<string, ArtistViewModel>(),
            new[] { "Jazz", "Rock" },
            new AlbumViewModel(),
            new ArtistViewModel());

        var libraryContext = new LibraryContext { Music = musicLibrary };
        var settings = new TestSettingsService();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        vm.FetchAlbums();
        await Assert.That(vm.Songs.Count).IsEqualTo(3);
        await Assert.That(vm.Genres).Contains("Jazz");
        await Assert.That(vm.Genres).Contains("Rock");
        await Assert.That(vm.GroupedAlbums.Sum(g => g.Count)).IsEqualTo(3);

        vm.SelectedGenre = "Rock";
        await Assert.That(vm.Songs.Count).IsEqualTo(1);
        await Assert.That(vm.GroupedAlbums.Sum(g => g.Count)).IsEqualTo(1);
        await Assert.That(vm.GroupedAlbums.SelectMany(g => g).First().Name).IsEqualTo("Album Rock");

        vm.SelectedGenre = string.Empty;
        await Assert.That(vm.Songs.Count).IsEqualTo(1);
        await Assert.That(vm.GroupedAlbums.Sum(g => g.Count)).IsEqualTo(1);
        await Assert.That(vm.GroupedAlbums.SelectMany(g => g).First().Name).IsEqualTo("Album Unknown");

        vm.SelectedGenre = null;
        await Assert.That(vm.Songs.Count).IsEqualTo(3);
        await Assert.That(vm.GroupedAlbums.Sum(g => g.Count)).IsEqualTo(3);
    }

    [Test]
    public void Receive_ShouldNotThrow()
    {
        var settings = new TestSettingsService();
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        var message = new PropertyChangedMessage<MusicLibrary>(
            libraryContext,
            nameof(LibraryContext.Music),
            libraryContext.Music,
            libraryContext.Music);

        vm.Receive(message);
    }

    [Test]
    public async Task FetchAlbums_ShouldUpdateGroupedAlbums()
    {
        var settings = new TestSettingsService();
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        await Assert.That(vm.GroupedAlbums).IsEmpty();

        var album = new AlbumViewModel("Album A", "Artist A");
        var albums = new Dictionary<string, AlbumViewModel> { ["Album A"] = album };
        var newLibrary = new MusicLibrary(
            Array.Empty<MediaViewModel>(),
            albums,
            new Dictionary<string, ArtistViewModel>(),
            new AlbumViewModel(),
            new ArtistViewModel());

        libraryContext.Music = newLibrary;
        vm.FetchAlbums();

        await Assert.That(vm.GroupedAlbums).IsNotEmpty();
        await Assert.That(vm.GroupedAlbums.SelectMany(g => g)).Contains(album);
    }

    private static MediaViewModel CreateSong(string name, string genre)
    {
        var info = new MediaInfo(MediaPlaybackType.Music, name);
        info.MusicProperties.Genre = genre;
        return new MediaViewModel(new PlayerContext(), null!, new Uri($"file:///c:/music/{name}.mp3"))
        {
            Name = name,
            MediaInfo = info
        };
    }
}
