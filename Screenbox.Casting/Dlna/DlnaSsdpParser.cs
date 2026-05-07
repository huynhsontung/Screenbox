#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Screenbox.Casting.Dlna;

/// <summary>
/// Parses SSDP M-SEARCH responses and UPnP device description XML documents
/// to extract the information needed to build a <see cref="DlnaDevice"/>.
/// </summary>
internal static class DlnaSsdpParser
{
    /// <summary>
    /// Parses a raw SSDP response string into header key-value pairs.
    /// Keys are normalised to lower-case.
    /// </summary>
    /// <param name="response">The raw text of the SSDP UDP response datagram.</param>
    /// <returns>A dictionary of header name → value pairs.</returns>
    internal static Dictionary<string, string> ParseSsdpHeaders(string response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using StringReader reader = new(response);

        // Skip the first line (e.g. "HTTP/1.1 200 OK").
        reader.ReadLine();

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex <= 0) continue;

            string key = line.Substring(0, colonIndex).Trim();
            string value = line.Substring(colonIndex + 1).Trim();
            headers[key] = value;
        }

        return headers;
    }

    /// <summary>
    /// Parses a UPnP device description XML document and extracts the device friendly name,
    /// AVTransport control URL, and RenderingControl control URL.
    /// </summary>
    /// <param name="descriptionXml">The XML content of the device description document.</param>
    /// <param name="baseUrl">
    /// The base URL (scheme + host + port) of the device, used to resolve relative control URLs.
    /// </param>
    /// <returns>
    /// A tuple of (friendlyName, avTransportControlUrl, renderingControlUrl), or <c>null</c>
    /// if the document is not a valid UPnP MediaRenderer description.
    /// </returns>
    internal static (string FriendlyName, Uri AvTransportControlUrl, Uri RenderingControlUrl)?
        ParseDeviceDescription(string descriptionXml, Uri baseUrl)
    {
        try
        {
            XmlDocument doc = new();

            // Disable DTD processing to prevent XML external entity attacks (XXE).
            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            using MemoryStream ms = new(Encoding.UTF8.GetBytes(descriptionXml));
            using XmlReader xmlReader = XmlReader.Create(ms, settings);
            doc.Load(xmlReader);

            // UPnP device description documents use a default namespace.
            XmlNamespaceManager ns = new(doc.NameTable);
            ns.AddNamespace("upnp", "urn:schemas-upnp-org:device-1-0");

            string? friendlyName = doc.SelectSingleNode("//upnp:device/upnp:friendlyName", ns)?.InnerText?.Trim();
            if (string.IsNullOrEmpty(friendlyName)) return null;

            // Find AVTransport and RenderingControl service control URLs.
            string? avTransportUrl = FindServiceControlUrl(doc, ns, "urn:schemas-upnp-org:service:AVTransport:", baseUrl);
            string? renderingControlUrl = FindServiceControlUrl(doc, ns, "urn:schemas-upnp-org:service:RenderingControl:", baseUrl);

            if (avTransportUrl is null || renderingControlUrl is null) return null;

            if (!Uri.TryCreate(avTransportUrl, UriKind.Absolute, out Uri? avUri)) return null;
            if (!Uri.TryCreate(renderingControlUrl, UriKind.Absolute, out Uri? rcUri)) return null;

            return (friendlyName, avUri, rcUri);
        }
        catch (Exception)
        {
            // Malformed or unexpected XML — skip this device.
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Finds the control URL for a UPnP service whose <c>serviceType</c> starts with
    /// <paramref name="serviceTypePrefix"/>, and resolves it against <paramref name="baseUrl"/>.
    /// </summary>
    private static string? FindServiceControlUrl(XmlDocument doc, XmlNamespaceManager ns, string serviceTypePrefix, Uri baseUrl)
    {
        XmlNodeList? services = doc.SelectNodes("//upnp:serviceList/upnp:service", ns);
        if (services is null) return null;

        foreach (XmlNode service in services)
        {
            string? serviceType = service.SelectSingleNode("upnp:serviceType", ns)?.InnerText;
            if (serviceType is null || !serviceType.StartsWith(serviceTypePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? controlUrl = service.SelectSingleNode("upnp:controlURL", ns)?.InnerText?.Trim();
            if (string.IsNullOrEmpty(controlUrl)) continue;

            // Resolve relative URLs against the device base URL.
            return Uri.TryCreate(baseUrl, controlUrl, out Uri? resolved)
                ? resolved.ToString()
                : null;
        }

        return null;
    }
}
