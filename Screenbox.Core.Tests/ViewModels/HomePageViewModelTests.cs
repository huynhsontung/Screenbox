using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Data;

namespace Screenbox.Core.Tests.ViewModels;

public sealed class HomePageViewModelTests
{
    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private sealed class DummyCoordinator : IRecentMediaCoordinator
    {
        public Task LoadAsync() => Task.CompletedTask;
        public Task LoadDetailsAndThumbnailsAsync() => Task.CompletedTask;
        public Task RemoveAsync(RecentItemViewModel item) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
    }

    [Test]
    public async Task PlaySelectedNext_QueuesSelectedItemsInNaturalForwardOrder()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext();
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var settingsStorage = new MemorySettingsStorage();
        var generalSettings = new GeneralSettingsContext(settingsStorage);

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        var track1 = CreateDummyMedia("Track 1");
        var track2 = CreateDummyMedia("Track 2");
        var track3 = CreateDummyMedia("Track 3");

        var album = new AlbumViewModel("Album 1", "Artist 1", messenger);
        album.RelatedSongs.Add(track1);
        album.RelatedSongs.Add(track2);

        var recentAlbum = new RecentItemViewModel(
            "entry_album",
            "hash_album",
            QueueOrigin.ForAlbum(album));
        recentAlbum.LinkAlbum(album);

        var recentSingle = new RecentItemViewModel(
            "entry_single",
            "hash_single",
            track3);

        recentContext.Recent.Add(recentAlbum);
        recentContext.Recent.Add(recentSingle);

        // Select both items via SelectionViewModel
        selection.IsSelectionModeActive = true;
        selection.SelectRange(new ItemIndexRange(0, 2));

        IReadOnlyList<MediaViewModel>? queuedItems = null;

        messenger.Register<QueueRequestMessage>(this, (_, msg) =>
        {
            msg.Reply(new QueueSession(0, []));
        });

        messenger.Register<SetQueueMessage>(this, (_, msg) =>
        {
            queuedItems = msg.Value.GetCanonicalItems();
        });

        vm.PlaySelectedNextCommand.Execute(null);

        await Assert.That(queuedItems).IsNotNull();
        await Assert.That(queuedItems!.Count).IsEqualTo(3);
        await Assert.That(queuedItems[0].Name).IsEqualTo("Track 1");
        await Assert.That(queuedItems[1].Name).IsEqualTo("Track 2");
        await Assert.That(queuedItems[2].Name).IsEqualTo("Track 3");
    }
}
