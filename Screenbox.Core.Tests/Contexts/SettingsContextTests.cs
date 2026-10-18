using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;

namespace Screenbox.Core.Tests.Contexts;

public class SettingsContextTests
{
    [Test]
    public async Task PlayerSettingsContext_WhenPropertyChanged_UpdatesStorageAndNotifies()
    {
        var storage = new MemorySettingsStorage();
        var context = new PlayerSettingsContext(storage);

        int changeCount = 0;
        string? changedProperty = null;
        context.PropertyChanged += (s, e) =>
        {
            changeCount++;
            changedProperty = e.PropertyName;
        };

        context.RewindStep = 15;
        context.RewindStep = 15;

        await Assert.That(context.RewindStep).IsEqualTo(15);
        await Assert.That(storage.GetValue("Player/RewindStep", 0)).IsEqualTo(15);
        await Assert.That(changedProperty).IsEqualTo(nameof(PlayerSettingsContext.RewindStep));
        await Assert.That(changeCount).IsEqualTo(1);
    }

    [Test]
    public async Task GeneralSettingsContext_WhenThemeChanged_UpdatesStorageAndBroadcastsMessage()
    {
        var storage = new MemorySettingsStorage();
        var context = new GeneralSettingsContext(storage);

        PropertyChangedMessage<ThemeOption>? receivedMessage = null;
        SettingChangedMessage<GeneralSettingsContext, ThemeOption>? receivedDomainMessage = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<PropertyChangedMessage<ThemeOption>>(
            recipient,
            (r, m) =>
            {
                if (ReferenceEquals(m.Sender, context))
                {
                    receivedMessage = m;
                }
            });
        WeakReferenceMessenger.Default.Register<SettingChangedMessage<GeneralSettingsContext, ThemeOption>>(
            recipient,
            (r, m) =>
            {
                if (ReferenceEquals(m.Context, context))
                {
                    receivedDomainMessage = m;
                }
            });

        try
        {
            context.Theme = ThemeOption.Dark;

            await Assert.That(context.Theme).IsEqualTo(ThemeOption.Dark);
            await Assert.That(storage.GetValue("General/Theme", 0)).IsEqualTo((int)ThemeOption.Dark);
            await Assert.That(receivedMessage).IsNotNull();
            await Assert.That(receivedMessage!.NewValue).IsEqualTo(ThemeOption.Dark);
            await Assert.That(receivedDomainMessage).IsNotNull();
            await Assert.That(receivedDomainMessage!.NewValue).IsEqualTo(ThemeOption.Dark);
            await Assert.That(receivedDomainMessage!.PropertyName).IsEqualTo(nameof(GeneralSettingsContext.Theme));
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Test]
    public async Task SettingChangedMessage_RoutesExclusivelyToMatchingContextDomain()
    {
        var storage = new MemorySettingsStorage();
        var playerContext = new PlayerSettingsContext(storage);
        var generalContext = new GeneralSettingsContext(storage);

        int playerCount = 0;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<SettingChangedMessage<PlayerSettingsContext, bool>>(
            recipient,
            (r, m) => playerCount++);

        try
        {
            generalContext.ShowRecent = false;
            await Assert.That(playerCount).IsEqualTo(0);

            playerContext.AdvancedMode = true;
            await Assert.That(playerCount).IsEqualTo(1);
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Test]
    public async Task PlaybackSettingsContext_WhenPositionTrackingChanged_UpdatesStorage()
    {
        var storage = new MemorySettingsStorage();
        var context = new PlaybackSettingsContext(storage);

        context.PersistPlaybackPosition = false;
        context.RestorePlaybackPosition = true;

        await Assert.That(context.PersistPlaybackPosition).IsFalse();
        await Assert.That(context.RestorePlaybackPosition).IsTrue();
        await Assert.That(storage.GetValue("Privacy/PersistPlaybackPosition", true)).IsFalse();
        await Assert.That(storage.GetValue("General/RestorePlaybackPosition", false)).IsTrue();
    }

    [Test]
    public async Task PlayerSettingsContext_GlobalArguments_SanitizesValue()
    {
        var storage = new MemorySettingsStorage();
        var context = new PlayerSettingsContext(storage);

        context.GlobalArguments = "invalid -v --network-caching=1000 -- trailing";

        await Assert.That(context.GlobalArguments).IsEqualTo("-v --network-caching=1000");
        await Assert.That(storage.GetValue("Values/GlobalArguments", string.Empty)).IsEqualTo("-v --network-caching=1000");
    }
}
