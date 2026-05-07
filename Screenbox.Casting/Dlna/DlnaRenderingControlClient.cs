#nullable enable

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Screenbox.Casting.Dlna;

/// <summary>
/// Sends UPnP RenderingControl SOAP actions to a DLNA Digital Media Renderer (DMR) device.
/// </summary>
/// <remarks>
/// DLNA volume is a 0–100 integer.  All public methods accept / return a 0.0–1.0
/// normalised <see cref="double"/> and convert internally.
/// </remarks>
internal sealed class DlnaRenderingControlClient
{
    private const string RenderingControlServiceType = "urn:schemas-upnp-org:service:RenderingControl:1";
    private const string InstanceId = "0";
    private const string Channel = "Master"; // Default audio channel.

    private readonly Uri _controlUrl;
    private readonly HttpClient _httpClient;

    internal DlnaRenderingControlClient(Uri controlUrl, HttpClient httpClient)
    {
        _controlUrl = controlUrl;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Sets the volume to <paramref name="normalised"/> (0.0–1.0).
    /// The value is clamped before sending.
    /// </summary>
    internal Task SetVolumeAsync(double normalised)
    {
        int dlnaVolume = NormaliseToDlna(normalised);
        return PostSoapAsync("SetVolume",
            $"<InstanceID>{InstanceId}</InstanceID>" +
            $"<Channel>{Channel}</Channel>" +
            $"<DesiredVolume>{dlnaVolume}</DesiredVolume>");
    }

    /// <summary>
    /// Queries the current volume level.
    /// </summary>
    /// <returns>A normalised 0.0–1.0 volume level, or <c>null</c> if unavailable.</returns>
    internal async Task<double?> GetVolumeAsync()
    {
        try
        {
            string response = await PostSoapAsync("GetVolume",
                $"<InstanceID>{InstanceId}</InstanceID>" +
                $"<Channel>{Channel}</Channel>",
                returnResponse: true).ConfigureAwait(false);

            string? raw = ExtractResponseValue(response, "CurrentVolume");
            if (raw is not null && int.TryParse(raw, out int dlnaVolume))
            {
                return Math.Clamp(dlnaVolume / 100.0, 0.0, 1.0);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Sets the mute state.</summary>
    internal Task SetMuteAsync(bool muted) =>
        PostSoapAsync("SetMute",
            $"<InstanceID>{InstanceId}</InstanceID>" +
            $"<Channel>{Channel}</Channel>" +
            $"<DesiredMute>{(muted ? "1" : "0")}</DesiredMute>");

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds and posts a SOAP request to the RenderingControl control URL.
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
                    $"<u:{action} xmlns:u=\"{RenderingControlServiceType}\">" +
                        bodyArguments +
                    $"</u:{action}>" +
                "</s:Body>" +
            "</s:Envelope>";

        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"\"{RenderingControlServiceType}#{action}\"");

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
    /// Converts a normalised 0.0–1.0 level to the DLNA 0–100 integer range.
    /// </summary>
    private static int NormaliseToDlna(double normalised) =>
        (int)Math.Round(Math.Clamp(normalised, 0.0, 1.0) * 100.0);
}
