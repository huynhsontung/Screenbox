using System;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Screenbox.Core;
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
    public async Task PopulateCoreServices_RegistersKeyedMediaProbeServices()
    {
        ServiceCollection services = new();
        ServiceHelpers.PopulateCoreServices(services);
        services.AddLogging();
        services.AddSingleton<IVlcDialogService, DummyVlcDialogService>();

        using ServiceProvider provider = services.BuildServiceProvider();

        IMediaProbeService? storageProbe = provider.GetKeyedService<IMediaProbeService>(typeof(StorageMediaProbeService));
        IMediaProbeService? vlcProbe = provider.GetKeyedService<IMediaProbeService>(typeof(VlcMediaProbeService));

        await Assert.That(storageProbe).IsNotNull();
        await Assert.That(storageProbe).IsTypeOf<StorageMediaProbeService>();

        await Assert.That(vlcProbe).IsNotNull();
        await Assert.That(vlcProbe).IsTypeOf<VlcMediaProbeService>();
    }

    [Test]
    public async Task PopulateCoreServices_ResolvesPropertyViewModelWithKeyedServices()
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
