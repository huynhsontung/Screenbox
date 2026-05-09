#nullable enable

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Screenbox.Casting.Dlna;

/// <summary>
/// Sends UPnP AVTransport SOAP actions to a DLNA Digital Media Renderer (DMR) device.
/// </summary>
/// <remarks>
/// Supported actions: <c>SetAVTransportURI</c>, <c>Play</c>, <c>Pause</c>, <c>Stop</c>,
/// <c>Seek</c>, <c>GetPositionInfo</c>, and <c>GetTransportInfo</c>.
/// All methods are fire-and-complete; callers are responsible for handling exceptions.
/// </remarks>
internal sealed class DlnaAvTransportClient
{
    private const string AvTransportServiceType = "urn:schemas-upnp-org:service:AVTransport:1";
    private const string InstanceId = "0"; // Always 0 for single-zone renderers.

    private readonly Uri _controlUrl;
    private readonly HttpClient _httpClient;

    internal DlnaAvTransportClient(Uri controlUrl, HttpClient httpClient)
    {
        _controlUrl = controlUrl;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Sets the URI of the media to play on the renderer.
    /// Call this before <see cref="PlayAsync"/>.
    /// </summary>
    /// <param name="uri">The HTTP URI of the media stream.</param>
    /// <param name="metadata">Optional DIDL-Lite metadata string (may be empty).</param>
    internal Task SetAvTransportUriAsync(string uri, string metadata = "") =>
        PostSoapAsync("SetAVTransportURI",
            $"<InstanceID>{InstanceId}</InstanceID>" +
            $"<CurrentURI>{EscapeXml(uri)}</CurrentURI>" +
            $"<CurrentURIMetaData>{EscapeXml(metadata)}</CurrentURIMetaData>");

    /// <summary>Sends the Play command to start or resume playback.</summary>
    internal Task PlayAsync() =>
        PostSoapAsync("Play",
            $"<InstanceID>{InstanceId}</InstanceID>" +
            "<Speed>1</Speed>");

    /// <summary>Sends the Pause command.</summary>
    internal Task PauseAsync() =>
        PostSoapAsync("Pause",
            $"<InstanceID>{InstanceId}</InstanceID>");

    /// <summary>Sends the Stop command.</summary>
    internal Task StopAsync() =>
        PostSoapAsync("Stop",
            $"<InstanceID>{InstanceId}</InstanceID>");

    /// <summary>
    /// Seeks to the specified position.
    /// </summary>
    /// <param name="position">The target position.</param>
    internal Task SeekAsync(TimeSpan position) =>
        PostSoapAsync("Seek",
            $"<InstanceID>{InstanceId}</InstanceID>" +
            "<Unit>REL_TIME</Unit>" +
            $"<Target>{FormatPosition(position)}</Target>");

    /// <summary>
    /// Queries the current playback position and total track duration.
    /// </summary>
    /// <returns>
    /// A tuple of (position, duration) in seconds, or <c>null</c> if the query fails
    /// or the response cannot be parsed.
    /// </returns>
    internal async Task<(double Position, double Duration)?> GetPositionInfoAsync()
    {
        try
        {
            string response = await PostSoapAsync("GetPositionInfo",
                $"<InstanceID>{InstanceId}</InstanceID>",
                returnResponse: true).ConfigureAwait(false);

            string? relTime = ExtractResponseValue(response, "RelTime");
            string? trackDuration = ExtractResponseValue(response, "TrackDuration");

            if (relTime is null || trackDuration is null) return null;

            double position = ParsePosition(relTime);
            double duration = ParsePosition(trackDuration);

            return (position, duration);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Queries the current transport state (e.g. PLAYING, PAUSED_PLAYBACK, STOPPED).
    /// </summary>
    /// <returns>The transport state string, or <c>null</c> if unavailable.</returns>
    internal async Task<string?> GetTransportInfoAsync()
    {
        try
        {
            string response = await PostSoapAsync("GetTransportInfo",
                $"<InstanceID>{InstanceId}</InstanceID>",
                returnResponse: true).ConfigureAwait(false);

            return ExtractResponseValue(response, "CurrentTransportState");
        }
        catch (Exception)
        {
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds and posts a SOAP request to the AVTransport control URL.
    /// When <paramref name="returnResponse"/> is <c>true</c> the response body is returned;
    /// otherwise an empty string is returned.
    /// </summary>
    private async Task<string> PostSoapAsync(string action, string bodyArguments, bool returnResponse = false)
    {
        string envelope =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                        "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                "<s:Body>" +
                    $"<u:{action} xmlns:u=\"{AvTransportServiceType}\">" +
                        bodyArguments +
                    $"</u:{action}>" +
                "</s:Body>" +
            "</s:Envelope>";

        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"\"{AvTransportServiceType}#{action}\"");

        using HttpResponseMessage httpResponse = await _httpClient
            .PostAsync(_controlUrl, content)
            .ConfigureAwait(false);

        httpResponse.EnsureSuccessStatusCode();

        return returnResponse
            ? await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false)
            : string.Empty;
    }

    /// <summary>
    /// Extracts the inner text of the first XML element matching <paramref name="elementName"/>
    /// from a SOAP response body string.
    /// </summary>
    private static string? ExtractResponseValue(string xml, string elementName)
    {
        try
        {
            XmlDocument doc = new();
            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            using System.IO.MemoryStream ms = new(Encoding.UTF8.GetBytes(xml));
            using XmlReader xmlReader = XmlReader.Create(ms, settings);
            doc.Load(xmlReader);

            XmlNode? node = doc.SelectSingleNode($"//{elementName}");
            return node?.InnerText?.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> as the UPnP REL_TIME string format
    /// <c>HH:MM:SS</c> (no sub-seconds).
    /// </summary>
    private static string FormatPosition(TimeSpan t) =>
        $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";

    /// <summary>
    /// Parses a UPnP REL_TIME / TRACK_DURATION string (<c>HH:MM:SS</c> or
    /// <c>HH:MM:SS.mmm</c>) to seconds. Returns 0 on failure.
    /// </summary>
    private static double ParsePosition(string value)
    {
        if (TimeSpan.TryParse(value, out TimeSpan ts)) return ts.TotalSeconds;

        // Some devices use "NOT_IMPLEMENTED" or "00:00:00" for unknown duration.
        return 0;
    }

    /// <summary>XML-escapes a string for embedding in SOAP element content.</summary>
    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;")
             .Replace("<", "&lt;")
             .Replace(">", "&gt;")
             .Replace("\"", "&quot;")
             .Replace("'", "&apos;");
}
