using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;

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

        await Assert.That(sourceService).IsNotNull();
        await Assert.That(sourceService).IsTypeOf<StorageMetadataService>();

        await Assert.That(playbackService).IsNotNull();
        await Assert.That(playbackService).IsTypeOf<VlcMetadataService>();
    }

    [Test]
    public async Task PopulateCoreServices_ResolvesPropertyViewModel()
    {
        ServiceCollection services = new();
        ServiceHelpers.PopulateCoreServices(services);
        services.AddLogging();
        services.AddSingleton<IVlcDialogService, DummyVlcDialogService>();

        using ServiceProvider provider = services.BuildServiceProvider();

        PropertyViewModel propertyVm = provider.GetRequiredService<PropertyViewModel>();
        await Assert.That(propertyVm).IsNotNull();
    }
}
