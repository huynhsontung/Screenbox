using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Screenbox.Core.Helpers;

/// <summary>
/// A global logger provider for static classes or non-DI components.
/// Uses the default IoC container to resolve the logger factory.
/// </summary>
public static class DefaultLogging
{
    private static ILoggerFactory? Factory => Ioc.Default.GetService<ILoggerFactory>();

    public static ILogger<T> CreateLogger<T>() => Ioc.Default.GetService<ILogger<T>>() ?? NullLogger<T>.Instance;

    public static ILogger CreateLogger(string categoryName) => Factory?.CreateLogger(categoryName) ?? NullLogger.Instance;
}
