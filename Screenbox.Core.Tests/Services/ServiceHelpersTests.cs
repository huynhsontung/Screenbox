using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Foundation.Collections;

namespace Screenbox.Core.Tests.Services;

public class ServiceHelpersTests
{
    private sealed class DummyVlcDialogService : IVlcDialogService
    {
        public void SetVlcDialogHandlers(LibVLC libVlc) { }
    }

    [Test]
    public async Task PopulateCoreServices_RegistersSourceAndPlaybackMetadataServices()
    {
        ServiceCollection services = new();
        ServiceHelpers.PopulateCoreServices(services);
        services.AddLogging();

        using ServiceProvider provider = services.BuildServiceProvider();

        ISourceMetadataService sourceService = provider.GetRequiredService<ISourceMetadataService>();
        IPlaybackMetadataService playbackService = provider.GetRequiredService<IPlaybackMetadataService>();
        IMediaThumbnailService thumbnailService = provider.GetRequiredService<IMediaThumbnailService>();

        await Assert.That(sourceService).IsNotNull();
        await Assert.That(sourceService).IsTypeOf<StorageMetadataService>();

        await Assert.That(playbackService).IsNotNull();
        await Assert.That(playbackService).IsTypeOf<VlcMetadataService>();

        await Assert.That(thumbnailService).IsNotNull();
        await Assert.That(thumbnailService).IsTypeOf<MediaThumbnailService>();

        ISubtitleService subtitleService = provider.GetRequiredService<ISubtitleService>();
        await Assert.That(subtitleService).IsNotNull();
        await Assert.That(subtitleService).IsTypeOf<SubtitleService>();
    }

    [Test]
    public async Task PopulateCoreServices_ResolvesPropertyViewModel()
    {
        ServiceCollection services = new();
        ServiceHelpers.PopulateCoreServices(services);
        services.AddLogging();
        services.AddSingleton<IVlcDialogService, DummyVlcDialogService>();
        services.AddSingleton<ISettingsStorage>(new SettingsStorage(new PropertySet()));

        using ServiceProvider provider = services.BuildServiceProvider();

        PropertyViewModel propertyVm = provider.GetRequiredService<PropertyViewModel>();
        await Assert.That(propertyVm).IsNotNull();
    }
}
