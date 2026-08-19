using Screenbox.Core.Contexts;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class SongsPageViewModelTests
{
    [Test]
    public async Task Constructor_ShouldInitializeSortByFromSettings()
    {
        var settings = new TestSettingsService { PersistentSongsSortBy = "artist" };
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        await Assert.That(vm.SortBy).IsEqualTo("artist");
    }

    [Test]
    public async Task SortBy_WhenChanged_ShouldUpdatePersistentSettings()
    {
        var settings = new TestSettingsService { PersistentSongsSortBy = "title" };
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        vm.SortBy = "album";

        await Assert.That(settings.PersistentSongsSortBy).IsEqualTo("album");
    }

    [Test]
    public async Task SetSortByCommand_WhenExecuted_ShouldUpdateSortByAndSettings()
    {
        var settings = new TestSettingsService { PersistentSongsSortBy = "title" };
        var libraryContext = new LibraryContext();
        var vm = new SongsPageViewModel(libraryContext, settings);

        vm.SetSortByCommand.Execute("dateAdded");

        await Assert.That(vm.SortBy).IsEqualTo("dateAdded");
        await Assert.That(settings.PersistentSongsSortBy).IsEqualTo("dateAdded");
    }
}
