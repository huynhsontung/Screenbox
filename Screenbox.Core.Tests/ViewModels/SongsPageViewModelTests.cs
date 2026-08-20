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

public class SongsPageViewModelTests
{
    [Test]
    public async Task Constructor_ShouldInitializeSortByFromSettings()
    {
        var settings = new TestSettingsService { PersistentSongsSortOrder = SongSortOrder.Artist };
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        await Assert.That(vm.SortBy).IsEqualTo(SongSortOrder.Artist);
        await Assert.That(vm.SelectedGenre).IsNull();
    }

    [Test]
    public async Task SortBy_WhenChanged_ShouldUpdatePersistentSettings()
    {
        var settings = new TestSettingsService { PersistentSongsSortOrder = SongSortOrder.Title };
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        vm.SortBy = SongSortOrder.Album;

        await Assert.That(settings.PersistentSongsSortOrder).IsEqualTo(SongSortOrder.Album);
    }

    [Test]
    public async Task SetSortByCommand_WhenExecuted_ShouldUpdateSortByAndSettings()
    {
        var settings = new TestSettingsService { PersistentSongsSortOrder = SongSortOrder.Title };
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        vm.SetSortByCommand.Execute(SongSortOrder.DateAdded);

        await Assert.That(vm.SortBy).IsEqualTo(SongSortOrder.DateAdded);
        await Assert.That(settings.PersistentSongsSortOrder).IsEqualTo(SongSortOrder.DateAdded);
    }

    [Test]
    public async Task SetGenreCommand_WhenExecuted_ShouldUpdateSelectedGenre()
    {
        var settings = new TestSettingsService();
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        vm.SetGenreCommand.Execute("Rock");
        await Assert.That(vm.SelectedGenre).IsEqualTo("Rock");

        vm.SetGenreCommand.Execute(string.Empty);
        await Assert.That(vm.SelectedGenre).IsEqualTo(string.Empty);

        vm.SetGenreCommand.Execute(null);
        await Assert.That(vm.SelectedGenre).IsNull();
    }

    [Test]
    public async Task FetchSongs_WhenFilteredByGenre_ShouldFilterSongsAndGroupedSongs()
    {
        var song1 = CreateSong("Song A", "Rock");
        var song2 = CreateSong("Song B", "Pop");
        var song3 = CreateSong("Song C", "");

        var songs = new List<MediaViewModel> { song1, song2, song3 };
        var musicLibrary = new MusicLibrary(
            songs,
            new Dictionary<string, AlbumViewModel>(),
            new Dictionary<string, ArtistViewModel>(),
            new[] { "Pop", "Rock" },
            new AlbumViewModel(),
            new ArtistViewModel());

        var libraryContext = new LibraryContext { Music = musicLibrary };
        var settings = new TestSettingsService();
        var vm = new SongsPageViewModel(libraryContext, settings);

        vm.FetchSongs();
        await Assert.That(vm.Songs.Count).IsEqualTo(3);
        await Assert.That(vm.Genres).Contains("Pop");
        await Assert.That(vm.Genres).Contains("Rock");

        vm.SelectedGenre = "Rock";
        await Assert.That(vm.Songs.Count).IsEqualTo(1);
        await Assert.That(vm.Songs[0].Name).IsEqualTo("Song A");

        vm.SelectedGenre = string.Empty;
        await Assert.That(vm.Songs.Count).IsEqualTo(1);
        await Assert.That(vm.Songs[0].Name).IsEqualTo("Song C");

        vm.SelectedGenre = null;
        await Assert.That(vm.Songs.Count).IsEqualTo(3);
    }

    [Test]
    public void Receive_ShouldNotThrow()
    {
        var settings = new TestSettingsService();
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        var message = new PropertyChangedMessage<MusicLibrary>(
            libraryContext,
            nameof(LibraryContext.Music),
            libraryContext.Music,
            libraryContext.Music);

        vm.Receive(message);
    }

    [Test]
    public async Task FetchSongs_ShouldUpdateSongsAndGroupedSongs()
    {
        var settings = new TestSettingsService();
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        await Assert.That(vm.Songs).IsEmpty();
        await Assert.That(vm.GroupedSongs).IsEmpty();

        var song = new MediaViewModel(new PlayerContext(), new TestPlayerService(), new Uri("file:///c:/music/song.mp3"))
        {
            Name = "Song A"
        };
        var newLibrary = new MusicLibrary(
            new[] { song },
            new Dictionary<string, AlbumViewModel>(),
            new Dictionary<string, ArtistViewModel>(),
            new AlbumViewModel(),
            new ArtistViewModel());

        libraryContext.Music = newLibrary;
        vm.FetchSongs();

        await Assert.That(vm.Songs.Count).IsEqualTo(1);
        await Assert.That(vm.GroupedSongs).IsNotEmpty();
        await Assert.That(vm.GroupedSongs.SelectMany(g => g)).Contains(song);
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
