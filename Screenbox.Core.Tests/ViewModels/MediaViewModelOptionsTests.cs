using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class MediaViewModelOptionsTests
{
    private static MediaViewModel CreateMedia(string name = "test") =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp4"))) { Name = name };

    [Test]
    public async Task SetOptions_ValidColonOptions_StoredSuccessfully()
    {
        var media = CreateMedia();
        media.SetOptions(":network-caching=5000 :aspect-ratio=16:9");

        await Assert.That(media.Options.Count).IsEqualTo(2);
        await Assert.That(media.Options[0]).IsEqualTo(":network-caching=5000");
        await Assert.That(media.Options[1]).IsEqualTo(":aspect-ratio=16:9");
    }

    [Test]
    public async Task SetOptions_InvalidTokensAndWhitespace_FiltersNonColonArgs()
    {
        var media = CreateMedia();
        media.SetOptions("  invalidArg   :valid-opt=1   --also-invalid : :audio-track=2   ");

        await Assert.That(media.Options.Count).IsEqualTo(2);
        await Assert.That(media.Options[0]).IsEqualTo(":valid-opt=1");
        await Assert.That(media.Options[1]).IsEqualTo(":audio-track=2");
    }

    [Test]
    public async Task SetOptions_EmptyString_ClearsOptions()
    {
        var media = CreateMedia();
        media.SetOptions(":network-caching=1000");
        await Assert.That(media.Options.Count).IsEqualTo(1);

        media.SetOptions(string.Empty);
        await Assert.That(media.Options.Count).IsEqualTo(0);
    }

    [Test]
    public async Task UpdateMediaOptionsMessage_DeliversAndSetsOptionsViaMessenger()
    {
        var messenger = new WeakReferenceMessenger();
        var media = CreateMedia();

        messenger.Register<MediaViewModelOptionsTests, UpdateMediaOptionsMessage>(this, (_, msg) =>
        {
            var (targetMedia, rawOptions) = msg;
            targetMedia.SetOptions(rawOptions);
        });

        messenger.Send(new UpdateMediaOptionsMessage(media, ":start-time=30 :sub-delay=100"));

        await Assert.That(media.Options.Count).IsEqualTo(2);
        await Assert.That(media.Options[0]).IsEqualTo(":start-time=30");
        await Assert.That(media.Options[1]).IsEqualTo(":sub-delay=100");
    }
}
