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

        string? changedProperty = null;
        context.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        context.RewindStep = 15;

        await Assert.That(context.RewindStep).IsEqualTo(15);
        await Assert.That(storage.GetValue("Player/RewindStep", 0)).IsEqualTo(15);
        await Assert.That(changedProperty).IsEqualTo(nameof(PlayerSettingsContext.RewindStep));
    }

    [Test]
    public async Task GeneralSettingsContext_WhenThemeChanged_UpdatesStorage()
    {
        var storage = new MemorySettingsStorage();
        var context = new GeneralSettingsContext(storage);

        context.Theme = ThemeOption.Dark;

        await Assert.That(context.Theme).IsEqualTo(ThemeOption.Dark);
        await Assert.That(storage.GetValue("General/Theme", 0)).IsEqualTo((int)ThemeOption.Dark);
    }

    [Test]
    public async Task AdvancedSettingsContext_GlobalArguments_SanitizesValue()
    {
        var storage = new MemorySettingsStorage();
        var context = new AdvancedSettingsContext(storage);

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
            new GeneralSettingsContext(storage),
            new AdvancedSettingsContext(storage)
        );
        var service = new SettingsService(settingsContext);

        service.PlayerRewindStep = 20;
        service.Theme = ThemeOption.Light;
        service.UseIndexer = false;

        await Assert.That(settingsContext.Player.RewindStep).IsEqualTo(20);
        await Assert.That(settingsContext.General.Theme).IsEqualTo(ThemeOption.Light);
        await Assert.That(settingsContext.Library.UseIndexer).IsFalse();
    }
}
