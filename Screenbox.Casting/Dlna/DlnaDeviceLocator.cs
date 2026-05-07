#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Screenbox.Casting.Abstractions;
using Screenbox.Casting.Events;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Screenbox.Casting.Dlna;

/// <summary>
/// Discovers DLNA/UPnP Digital Media Renderer (DMR) devices on the local network
/// using the SSDP M-SEARCH protocol over a UWP <see cref="DatagramSocket"/>.
/// </summary>
/// <remarks>
/// <para>
/// SSDP discovery works by sending an M-SEARCH UDP multicast to <c>239.255.255.250:1900</c>
/// and listening for responses that advertise the
/// <c>urn:schemas-upnp-org:device:MediaRenderer:1</c> device type.
/// </para>
/// <para>
/// Each responding device's <c>LOCATION</c> header is fetched over HTTP to retrieve the
/// UPnP device description XML, from which the friendly name and service control URLs are
/// extracted by <see cref="DlnaSsdpParser"/>.
/// </para>
/// <para>
/// Requires the <c>privateNetworkClientServer</c> capability in the app manifest.
/// </para>
/// </remarks>
public sealed class DlnaDeviceLocator : ICastDeviceLocator
{
    private const string SsdpMulticastAddress = "239.255.255.250";
    private const string SsdpPort = "1900";

    // Search target for all UPnP MediaRenderer devices.
    private const string MediaRendererSearchTarget = "urn:schemas-upnp-org:device:MediaRenderer:1";

    // MX (maximum wait) in seconds — devices respond within this window.
    private const int SearchMx = 3;

    // How often to re-send M-SEARCH to catch newly appeared devices.
    private static readonly TimeSpan DiscoveryInterval = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public event EventHandler<CastDeviceFoundEventArgs>? DeviceFound;

    /// <inheritdoc/>
    public event EventHandler<CastDeviceRemovedEventArgs>? DeviceLost;

    /// <inheritdoc/>
    public bool IsStarted { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<ICastDevice> Devices => _devices.AsReadOnly();

    private readonly List<DlnaDevice> _devices;
    private readonly HttpClient _httpClient;
    private DatagramSocket? _socket;
    private System.Threading.Timer? _discoveryTimer;

    /// <summary>Initialises a new instance of <see cref="DlnaDeviceLocator"/>.</summary>
    public DlnaDeviceLocator()
    {
        _devices = new List<DlnaDevice>();
        _httpClient = new HttpClient();
        // Short timeout for device description fetches so slow/absent devices don't stall discovery.
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc/>
    public bool Start()
    {
        if (IsStarted) return false;

        IsStarted = true;

        // Fire-and-forget: start the socket and send the first M-SEARCH.
        _ = StartDiscoveryAsync();

        return true;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!IsStarted) return;

        IsStarted = false;

        _discoveryTimer?.Dispose();
        _discoveryTimer = null;

        CloseSocket();

        // Raise DeviceLost for every tracked device and mark them unavailable.
        foreach (DlnaDevice device in _devices)
        {
            device.MarkUnavailable();
            DeviceLost?.Invoke(this, new CastDeviceRemovedEventArgs(device));
        }

        _devices.Clear();
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="device"/> was not created by this locator.
    /// </exception>
    public async Task<ICastSession?> ConnectAsync(ICastDevice device, Uri streamUrl, TimeSpan startPosition)
    {
        if (device is not DlnaDevice dlnaDevice)
        {
            throw new ArgumentException("Device must be a DlnaDevice created by this locator.", nameof(device));
        }

        return await DlnaSession.CreateAsync(dlnaDevice, streamUrl, startPosition).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
    }

    // -------------------------------------------------------------------------
    // Discovery internals
    // -------------------------------------------------------------------------

    /// <summary>
    /// Opens the UDP socket, binds it, and sends the first M-SEARCH.
    /// A timer fires periodically to re-send M-SEARCH.
    /// </summary>
    private async Task StartDiscoveryAsync()
    {
        try
        {
            _socket = new DatagramSocket();
            _socket.MessageReceived += OnMessageReceived;

            // Bind to any local port so the OS assigns a free one.
            await _socket.BindServiceNameAsync(string.Empty).AsTask().ConfigureAwait(false);

            await SendMSearchAsync().ConfigureAwait(false);

            // Re-send M-SEARCH on a timer so devices that appear later are found.
            _discoveryTimer = new System.Threading.Timer(
                async _ => await SendMSearchAsync().ConfigureAwait(false),
                state: null,
                dueTime: DiscoveryInterval,
                period: DiscoveryInterval);
        }
        catch (Exception)
        {
            // Discovery cannot start (e.g., network unavailable). Stop cleanly.
            Stop();
        }
    }

    /// <summary>
    /// Sends an SSDP M-SEARCH datagram to the multicast group.
    /// </summary>
    private async Task SendMSearchAsync()
    {
        if (_socket is null || !IsStarted) return;

        try
        {
            string message =
                "M-SEARCH * HTTP/1.1\r\n" +
                $"HOST: {SsdpMulticastAddress}:{SsdpPort}\r\n" +
                "MAN: \"ssdp:discover\"\r\n" +
                $"MX: {SearchMx}\r\n" +
                $"ST: {MediaRendererSearchTarget}\r\n" +
                "\r\n";

            IOutputStream outputStream = await _socket
                .GetOutputStreamAsync(new HostName(SsdpMulticastAddress), SsdpPort)
                .AsTask()
                .ConfigureAwait(false);

            using DataWriter writer = new(outputStream);
            writer.WriteBytes(Encoding.UTF8.GetBytes(message));
            await writer.StoreAsync().AsTask().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Network error — silently ignore and let the timer retry.
        }
    }

    /// <summary>
    /// Handles incoming UDP datagrams (SSDP responses).
    /// Parses the LOCATION header, fetches the device description, and raises
    /// <see cref="DeviceFound"/> for new devices.
    /// </summary>
    private async void OnMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        if (!IsStarted) return;

        try
        {
            using DataReader reader = args.GetDataReader();
            reader.InputStreamOptions = InputStreamOptions.Partial;
            string message = reader.ReadString(reader.UnconsumedBufferLength);

            var headers = DlnaSsdpParser.ParseSsdpHeaders(message);

            // Only handle responses for MediaRenderer devices.
            if (!headers.TryGetValue("ST", out string? st) ||
                !st.Equals(MediaRendererSearchTarget, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!headers.TryGetValue("LOCATION", out string? location) ||
                !Uri.TryCreate(location, UriKind.Absolute, out Uri? locationUri))
            {
                return;
            }

            // Deduplicate by location URL so multiple responses for the same device
            // do not result in duplicate entries.
            if (_devices.Any(d => d.UniqueId == location))
            {
                return;
            }

            // Fetch the device description XML from the LOCATION URL.
            DlnaDevice? device = await FetchDeviceAsync(locationUri).ConfigureAwait(false);
            if (device is null || !IsStarted) return;

            // Check for duplicates again (a race during fetch).
            if (_devices.Any(d => d.UniqueId == location)) return;

            _devices.Add(device);
            DeviceFound?.Invoke(this, new CastDeviceFoundEventArgs(device));
        }
        catch (Exception)
        {
            // Malformed datagram — ignore.
        }
    }

    /// <summary>
    /// Fetches and parses the UPnP device description from <paramref name="locationUri"/>.
    /// </summary>
    /// <returns>A <see cref="DlnaDevice"/> on success; <c>null</c> if parsing fails.</returns>
    private async Task<DlnaDevice?> FetchDeviceAsync(Uri locationUri)
    {
        try
        {
            string xml = await _httpClient.GetStringAsync(locationUri).ConfigureAwait(false);

            // Base URL is scheme + host + port (no path) for resolving relative service URLs.
            Uri baseUrl = new UriBuilder(locationUri.Scheme, locationUri.Host, locationUri.Port).Uri;

            var parsed = DlnaSsdpParser.ParseDeviceDescription(xml, baseUrl);
            if (parsed is null) return null;

            var (friendlyName, avTransportUrl, renderingControlUrl) = parsed.Value;

            return new DlnaDevice(
                name: friendlyName,
                avTransportControlUrl: avTransportUrl,
                renderingControlUrl: renderingControlUrl,
                uniqueId: locationUri.ToString());
        }
        catch (Exception)
        {
            // Device unreachable or returned invalid XML.
            return null;
        }
    }

    /// <summary>Closes and releases the UDP socket.</summary>
    private void CloseSocket()
    {
        if (_socket is not null)
        {
            _socket.MessageReceived -= OnMessageReceived;
            _socket.Dispose();
            _socket = null;
        }
    }
}
