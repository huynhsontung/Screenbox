using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
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

        try
        {
            context.Theme = ThemeOption.Dark;

            await Assert.That(context.Theme).IsEqualTo(ThemeOption.Dark);
            await Assert.That(storage.GetValue("General/Theme", 0)).IsEqualTo((int)ThemeOption.Dark);
            await Assert.That(receivedMessage).IsNotNull();
            await Assert.That(receivedMessage!.NewValue).IsEqualTo(ThemeOption.Dark);
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

    [Test]
    public async Task SettingsService_DelegatesToContexts()
    {
        var storage = new MemorySettingsStorage();
        var settingsContext = new SettingsContext(
            new PlayerSettingsContext(storage),
            new LibrarySettingsContext(storage),
            new PlaybackSettingsContext(storage),
            new GeneralSettingsContext(storage)
        );
        var service = new SettingsService(settingsContext);

        service.PlayerRewindStep = 20;
        service.Theme = ThemeOption.Light;
        service.UseIndexer = false;

        await Assert.That(settingsContext.Player.RewindStep).IsEqualTo(20);
        await Assert.That(settingsContext.General.Theme).IsEqualTo(ThemeOption.Light);
        await Assert.That(settingsContext.Library.UseIndexer).IsFalse();
        await Assert.That(storage.GetValue("Libraries/UseIndexer", true)).IsFalse();
    }
}
