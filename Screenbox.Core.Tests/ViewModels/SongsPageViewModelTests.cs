using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
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
}
