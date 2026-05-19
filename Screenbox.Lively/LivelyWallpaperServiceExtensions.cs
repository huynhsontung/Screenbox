using Microsoft.Extensions.DependencyInjection;
using Screenbox.Lively.Services;
using Screenbox.Lively.ViewModels;

namespace Screenbox.Lively;

/// <summary>
/// Provides extension methods to register services and view models required by the
/// <b>Lively Wallpaper</b> feature into an <see cref="IServiceCollection"/>.
/// </summary>
public static class LivelyWallpaperServiceExtensions
{
    /// <summary>
    /// Registers Lively Wallpaper view models and services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to which the Lively services will be added.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance so that additional method calls can be chained.</returns>
    public static IServiceCollection AddLivelyWallpaperServices(this IServiceCollection services)
    {
        // View models
        services.AddTransient<LivelyWallpaperPlayerViewModel>();
        services.AddTransient<LivelyWallpaperSelectorViewModel>();

        // Services
        services.AddSingleton<ILivelyWallpaperService, LivelyWallpaperService>();

        return services;
    }
}
