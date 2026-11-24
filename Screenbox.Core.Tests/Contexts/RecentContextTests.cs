using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.MediaSources;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.Contexts;

public class RecentContextTests
{
    [Test]
    public async Task RecentContext_InitialState_ShouldBeEmpty()
    {
        var context = new RecentContext(new StrongReferenceMessenger());

        await Assert.That(context.Recent).IsEmpty();
    }

    [Test]
    public async Task RecentContext_Recent_ShouldBroadcastPropertyChangedMessage()
    {
        var messenger = new StrongReferenceMessenger();
        var context = new RecentContext(messenger);
        string? changedProperty = null;
        PropertyChangedMessage<IReadOnlyList<IRecentItem>>? receivedMessage = null;

        context.PropertyChanged += (_, e) => changedProperty = e.PropertyName;
        messenger.Register<PropertyChangedMessage<IReadOnlyList<IRecentItem>>>(this, (_, m) => receivedMessage = m);

        var media = new MediaViewModel(new UriMediaSource(new Uri("https://example.com/song.mp3"))) { Name = "Song" };
        var item = new RecentMediaItemViewModel(media, messenger);
        IReadOnlyList<IRecentItem> snapshot = new IRecentItem[] { item };
        context.Recent = snapshot;

        await Assert.That(changedProperty).IsEqualTo(nameof(RecentContext.Recent));
        await Assert.That(receivedMessage).IsNotNull();
        await Assert.That(receivedMessage!.Sender).IsSameReferenceAs(context);
        await Assert.That(receivedMessage.NewValue).HasSingleItem();
        await Assert.That(receivedMessage.NewValue[0]).IsSameReferenceAs(item);
    }
}
