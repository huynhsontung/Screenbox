#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sharpcaster;
using Sharpcaster.Models;
using Screenbox.Core.Events;
using Screenbox.Core.Models;

namespace Screenbox.Core.Helpers;

/// <summary>
/// Watches for Chromecast devices on the local network using SharpCaster's mDNS discovery.
/// Discovery runs in a background loop, polling every <see cref="DiscoveryInterval"/>.
/// </summary>
public sealed class RendererWatcher : IDisposable
{
    public event EventHandler<RendererFoundEventArgs>? RendererFound;
    public event EventHandler<RendererLostEventArgs>? RendererLost;

    /// <summary>Gets a value indicating whether discovery is currently running.</summary>
    public bool IsStarted { get; private set; }

    /// <summary>How long to wait between discovery polls when devices are already found.</summary>
    private static readonly TimeSpan DiscoveryInterval = TimeSpan.FromSeconds(5);

    /// <summary>Timeout for each individual mDNS scan.</summary>
    private static readonly TimeSpan ScanTimeout = TimeSpan.FromSeconds(3);

    private readonly List<Renderer> _renderers;
    private CancellationTokenSource? _cts;

    internal RendererWatcher()
    {
        _renderers = new List<Renderer>();
    }

    /// <summary>Returns a read-only snapshot of all currently discovered renderers.</summary>
    public IReadOnlyList<Renderer> GetRenderers()
    {
        return _renderers.AsReadOnly();
    }

    /// <summary>Starts the background discovery loop. Returns <c>false</c> if already started.</summary>
    public bool Start()
    {
        if (IsStarted) return true;

        _cts = new CancellationTokenSource();
        // Fire-and-forget; errors are handled inside DiscoverAsync.
        _ = DiscoverAsync(_cts.Token);
        IsStarted = true;
        return true;
    }

    /// <summary>Stops the background discovery loop and marks all known renderers as unavailable.</summary>
    public void Stop()
    {
        if (!IsStarted) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        IsStarted = false;

        foreach (Renderer renderer in _renderers)
        {
            renderer.MarkUnavailable();
        }

        _renderers.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Background loop that periodically polls for Chromecast devices.
    /// On each iteration it compares the new scan results against the known list and
    /// raises <see cref="RendererFound"/> / <see cref="RendererLost"/> as appropriate.
    /// </summary>
    private async Task DiscoverAsync(CancellationToken cancellationToken)
    {
        var locator = new MdnsChromecastLocator();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Perform a time-bounded mDNS scan.
                using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                scanCts.CancelAfter(ScanTimeout);

                IEnumerable<ChromecastReceiver> found = await locator.FindReceiversAsync(scanCts.Token)
                    .ConfigureAwait(false);

                // Raise RendererFound for any newly discovered devices.
                foreach (ChromecastReceiver receiver in found)
                {
                    if (!_renderers.Any(r => r.Name == receiver.Name))
                    {
                        Renderer renderer = new(receiver);
                        _renderers.Add(renderer);
                        RendererFound?.Invoke(this, new RendererFoundEventArgs(renderer));
                    }
                }

                // Raise RendererLost for any devices no longer visible.
                List<Renderer> removed = _renderers
                    .Where(r => !found.Any(f => f.Name == r.Name))
                    .ToList();

                foreach (Renderer renderer in removed)
                {
                    _renderers.Remove(renderer);
                    renderer.MarkUnavailable();
                    RendererLost?.Invoke(this, new RendererLostEventArgs(renderer));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Outer cancellation — stop the loop cleanly.
                break;
            }
            catch (Exception)
            {
                // Swallow transient scan errors and retry on the next interval.
            }

            try
            {
                await Task.Delay(DiscoveryInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
