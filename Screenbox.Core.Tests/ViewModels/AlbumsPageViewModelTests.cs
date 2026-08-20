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
    public async Task Receive_ShouldEnqueueAndExecuteFetchAlbumsViaDispatcherQueue()
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
        DispatcherQueueTestHelper.PumpEvents();

        await Assert.That(vm.GroupedAlbums).IsNotEmpty();
        await Assert.That(vm.GroupedAlbums.SelectMany(g => g)).Contains(album);
    }
}
