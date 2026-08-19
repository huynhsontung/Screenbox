using Screenbox.Core.Contexts;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class AlbumsPageViewModelTests
{
    [Test]
    public async Task Constructor_ShouldInitializeSortByFromSettings()
    {
        var settings = new TestSettingsService { PersistentAlbumsSortBy = "year" };
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        await Assert.That(vm.SortBy).IsEqualTo("year");
    }

    [Test]
    public async Task SortBy_WhenChanged_ShouldUpdatePersistentSettings()
    {
        var settings = new TestSettingsService { PersistentAlbumsSortBy = "title" };
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        vm.SortBy = "artist";

        await Assert.That(settings.PersistentAlbumsSortBy).IsEqualTo("artist");
    }

    [Test]
    public async Task SetSortByCommand_WhenExecuted_ShouldUpdateSortByAndSettings()
    {
        var settings = new TestSettingsService { PersistentAlbumsSortBy = "title" };
        var libraryContext = new LibraryContext();
        var vm = new AlbumsPageViewModel(libraryContext, settings);

        vm.SetSortByCommand.Execute("dateAdded");

        await Assert.That(vm.SortBy).IsEqualTo("dateAdded");
        await Assert.That(settings.PersistentAlbumsSortBy).IsEqualTo("dateAdded");
    }
}
