using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;

namespace Screenbox.Core.Tests.ViewModels;

public sealed class RecentItemViewModelTests
{
    private static MediaViewModel CreateDummyMedia(string name, string caption = "")
    {
        var source = new UriMediaSource(new Uri($"https://example.com/{name}.mp3"));
        return new MediaViewModel(source)
        {
            Name = name,
            Caption = caption
        };
    }

    [Test]
    public async Task SingleMedia_ExposesCorrectStylingAndCaption()
    {
        var media = CreateDummyMedia("Track 1", "Artist A");
        var vm = new RecentItemViewModel("entry_1", "hash_1", media);

        await Assert.That(vm.Kind).IsEqualTo(RecentItemKind.SingleMedia);
        await Assert.That(vm.Name).IsEqualTo("Track 1");
        await Assert.That(vm.Caption).IsEqualTo("Artist A");
        await Assert.That(vm.CornerRadius).IsEqualTo(new CornerRadius(4));
        await Assert.That(vm.HorizontalTextAlignment).IsEqualTo(TextAlignment.Left);
    }

    [Test]
    public async Task Artist_ExposesCircularCornerRadiusAndCenterAlignment()
    {
        var vm = new RecentItemViewModel(
            "entry_artist",
            "hash_artist",
            RecentItemKind.Artist,
            "The Beatles",
            subtitle: null,
            sourceId: "beatles",
            snapshotItems: Array.Empty<MediaViewModel>());

        await Assert.That(vm.Kind).IsEqualTo(RecentItemKind.Artist);
        await Assert.That(vm.Name).IsEqualTo("The Beatles");
        await Assert.That(vm.Caption).IsEqualTo(string.Empty);
        await Assert.That(vm.CornerRadius).IsEqualTo(new CornerRadius(99));
        await Assert.That(vm.HorizontalTextAlignment).IsEqualTo(TextAlignment.Center);
    }

    [Test]
    public async Task Album_ExposesArtistAsCaptionAndStandardStyling()
    {
        var album = new AlbumViewModel("Abbey Road", "The Beatles");
        var vm = new RecentItemViewModel(
            "entry_album",
            "hash_album",
            RecentItemKind.Album,
            "Abbey Road",
            subtitle: "The Beatles",
            sourceId: "abbey_road",
            snapshotItems: Array.Empty<MediaViewModel>());

        vm.LinkAlbum(album);

        await Assert.That(vm.Kind).IsEqualTo(RecentItemKind.Album);
        await Assert.That(vm.Name).IsEqualTo("Abbey Road");
        await Assert.That(vm.Caption).IsEqualTo("The Beatles");
        await Assert.That(vm.CornerRadius).IsEqualTo(new CornerRadius(4));
        await Assert.That(vm.HorizontalTextAlignment).IsEqualTo(TextAlignment.Left);
    }

    [Test]
    public async Task SingleMedia_CaptionChange_NotifiesPropertyChanged()
    {
        var media = CreateDummyMedia("Track 1", "Initial");
        var vm = new RecentItemViewModel("entry_1", "hash_1", media);

        var changedProperties = new List<string>();
        vm.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName is not null)
            {
                changedProperties.Add(e.PropertyName);
            }
        };

        media.Caption = "Updated Artist";

        await Assert.That(vm.Caption).IsEqualTo("Updated Artist");
        await Assert.That(changedProperties).Contains(nameof(RecentItemViewModel.Caption));
    }
}
