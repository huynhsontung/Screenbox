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

        StorageMediaProbeService storageConcrete = provider.GetRequiredService<StorageMediaProbeService>();
        IMediaProbeService storageKeyed = provider.GetRequiredKeyedService<IMediaProbeService>(typeof(StorageMediaProbeService));

        VlcMediaProbeService vlcConcrete = provider.GetRequiredService<VlcMediaProbeService>();
        IMediaProbeService vlcKeyed = provider.GetRequiredKeyedService<IMediaProbeService>(typeof(VlcMediaProbeService));

        await Assert.That(storageKeyed).IsNotNull();
        await Assert.That(storageKeyed).IsTypeOf<StorageMediaProbeService>();
        await Assert.That(storageKeyed).IsSameReferenceAs(storageConcrete);

        await Assert.That(vlcKeyed).IsNotNull();
        await Assert.That(vlcKeyed).IsTypeOf<VlcMediaProbeService>();
        await Assert.That(vlcKeyed).IsSameReferenceAs(vlcConcrete);
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
