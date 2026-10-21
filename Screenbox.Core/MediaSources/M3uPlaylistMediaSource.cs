using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Core.MediaSources;

public class M3uPlaylistMediaSource : FileMediaSource, IExpandableMediaSource
{
    private readonly IMediaSourceFactory _sourceFactory;

    public M3uPlaylistMediaSource(StorageFile file, IMediaSourceFactory sourceFactory)
        : base(file)
    {
        _sourceFactory = sourceFactory;
    }

    public M3uPlaylistMediaSource(string path, IMediaSourceFactory sourceFactory)
        : base(path)
    {
        _sourceFactory = sourceFactory;
    }

    public async Task<IReadOnlyList<ExpandedMediaItem>> ExpandAsync(CancellationToken cancellationToken = default)
    {
        string content;
        try
        {
            var storageFile = await GetStorageFileAsync();
            if (storageFile != null)
            {
                using var stream = await storageFile.OpenStreamForReadAsync();
                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                content = await reader.ReadToEndAsync(cancellationToken);
            }
            else if (!string.IsNullOrEmpty(Path) && File.Exists(Path))
            {
                using var stream = File.OpenRead(Path);
                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                content = await reader.ReadToEndAsync(cancellationToken);
            }
            else
            {
                return Array.Empty<ExpandedMediaItem>();
            }
        }
        catch (Exception)
        {
            return Array.Empty<ExpandedMediaItem>();
        }

        // If the file is an HLS adaptive stream manifest rather than a static playlist of media,
        // do not expand into individual chunks; return empty so it plays directly.
        if (content.Contains("#EXT-X-TARGETDURATION", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<ExpandedMediaItem>();
        }

        var result = new List<ExpandedMediaItem>();
        Uri? baseUri = null;
        try
        {
            if (!string.IsNullOrEmpty(Path) && System.IO.Path.GetDirectoryName(Path) is { } directoryName)
            {
                baseUri = new Uri(directoryName.EndsWith(System.IO.Path.DirectorySeparatorChar)
                    ? directoryName
                    : directoryName + System.IO.Path.DirectorySeparatorChar, UriKind.Absolute);
            }
        }
        catch (Exception)
        {
            // pass
        }

        string extInfTitle = string.Empty;
        TimeSpan extInfDuration = default;

        foreach (var rawLine in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                (extInfTitle, extInfDuration) = ParseExtInf(line);
                continue;
            }

            if (line[0] == '#')
                continue;

            try
            {
                string targetPathOrUri = line;
                if (!Uri.TryCreate(line, UriKind.Absolute, out var uri))
                {
                    if (Uri.TryCreate(line, UriKind.Relative, out var relativeUri) && baseUri != null)
                    {
                        var resolvedUri = new Uri(baseUri, relativeUri);
                        targetPathOrUri = resolvedUri.IsFile ? resolvedUri.LocalPath : resolvedUri.AbsoluteUri;
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (uri.IsFile)
                {
                    targetPathOrUri = uri.LocalPath;
                }

                var source = _sourceFactory.Create(targetPathOrUri);
                result.Add(new ExpandedMediaItem(
                    source,
                    string.IsNullOrEmpty(extInfTitle) ? null : extInfTitle,
                    extInfDuration > TimeSpan.Zero ? extInfDuration : null));
            }
            catch (Exception)
            {
                continue;
            }
            finally
            {
                extInfTitle = string.Empty;
                extInfDuration = default;
            }
        }

        return result;
    }

    private static (string title, TimeSpan duration) ParseExtInf(string line)
    {
        const string prefix = "#EXTINF:";
        var body = line.Substring(prefix.Length);
        var commaIndex = body.IndexOf(',');
        if (commaIndex < 0)
            return (string.Empty, default);

        var title = body.Substring(commaIndex + 1).Trim();
        var durationPart = body.Substring(0, commaIndex).Trim();

        var spaceIndex = durationPart.IndexOf(' ');
        var durationStr = spaceIndex >= 0 ? durationPart.Substring(0, spaceIndex) : durationPart;

        TimeSpan duration = default;
        if (double.TryParse(durationStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            && seconds >= 0)
        {
            duration = TimeSpan.FromSeconds(seconds);
        }

        return (string.IsNullOrEmpty(title) ? string.Empty : title, duration);
    }
}
