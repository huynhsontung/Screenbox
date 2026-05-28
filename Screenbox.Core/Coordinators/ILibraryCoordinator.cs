#nullable enable

using System;
using System.Threading.Tasks;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Represents a stateful coordinator that owns library watchers and timers,
/// and drives library fetch operations through <see cref="Services.ILibraryService"/>.
/// </summary>
public interface ILibraryCoordinator : IDisposable
{
    /// <summary>
    /// Ensures query watchers are created and attached to the current context.
    /// Safe to call multiple times; no-ops if already watching.
    /// </summary>
    Task EnsureWatchingAsync();

    /// <summary>
    /// Recreates query watchers. Call when settings that affect queries (e.g. indexer usage) change.
    /// </summary>
    Task RefreshWatchersAsync();

    /// <summary>
    /// Fetches the music library and applies the result to <see cref="Contexts.LibraryContext"/>.
    /// </summary>
    Task FetchMusicAsync(bool useCache = true);

    /// <summary>
    /// Fetches the videos library and applies the result to <see cref="Contexts.LibraryContext"/>.
    /// </summary>
    Task FetchVideosAsync(bool useCache = true);
}
