using System;
using System.Collections.Generic;
using System.Linq;
using LibVLCSharp.Shared.Structures;
using Screenbox.Core.Models;
using Windows.Media.Core;

namespace Screenbox.Core.Helpers;

/// <summary>
/// Provides utility methods for sanitizing, validating, and normalizing media chapters.
/// </summary>
public static class ChapterHelper
{
    private const long MaxSupportedMilliseconds = long.MaxValue / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// Converts and sanitizes a sequence of LibVLC <see cref="ChapterDescription"/> items into a validated list of <see cref="ChapterCue"/> instances.
    /// </summary>
    /// <param name="vlcChapters">The raw LibVLC chapter descriptions, or <see langword="null"/>.</param>
    /// <param name="naturalDuration">The natural duration of the media stream.</param>
    /// <returns>A validated, chronologically sorted list of chapter cues.</returns>
    public static List<ChapterCue> FromVlcDescriptions(IEnumerable<ChapterDescription>? vlcChapters, TimeSpan naturalDuration)
    {
        if (vlcChapters is null)
        {
            return [];
        }

        List<ChapterInfo> rawChapters = [];
        foreach (ChapterDescription c in vlcChapters)
        {
            if (c.TimeOffset < 0 || c.TimeOffset > MaxSupportedMilliseconds)
            {
                continue;
            }

            TimeSpan startTime = TimeSpan.FromMilliseconds(c.TimeOffset);
            TimeSpan duration = c.Duration > 0 && c.Duration <= MaxSupportedMilliseconds
                ? TimeSpan.FromMilliseconds(c.Duration)
                : TimeSpan.Zero;

            rawChapters.Add(new ChapterInfo(startTime, duration, c.Name));
        }

        return Sanitize(rawChapters, naturalDuration);
    }

    /// <summary>
    /// Sanitizes and normalizes existing chapter cues by removing out-of-bounds chapters, calculating missing durations,
    /// preventing overlaps, and sorting chronologically.
    /// </summary>
    /// <param name="chapters">The source chapter cues, or <see langword="null"/>.</param>
    /// <param name="naturalDuration">The natural duration of the media stream.</param>
    /// <returns>A validated, chronologically sorted list of chapter cues.</returns>
    public static List<ChapterCue> Sanitize(IEnumerable<ChapterCue>? chapters, TimeSpan naturalDuration)
    {
        if (chapters is null)
        {
            return [];
        }

        IEnumerable<ChapterInfo> rawChapters = chapters.Select(c => new ChapterInfo(c.StartTime, c.Duration, c.Title));
        return Sanitize(rawChapters, naturalDuration);
    }

    /// <summary>
    /// Sanitizes and normalizes raw chapter info entries by removing out-of-bounds chapters, calculating missing durations,
    /// preventing overlaps, and sorting chronologically into platform <see cref="ChapterCue"/> instances.
    /// </summary>
    /// <param name="chapters">The raw chapter entries, or <see langword="null"/>.</param>
    /// <param name="naturalDuration">The natural duration of the media stream.</param>
    /// <returns>A validated, chronologically sorted list of chapter cues.</returns>
    public static List<ChapterCue> Sanitize(IEnumerable<ChapterInfo>? chapters, TimeSpan naturalDuration)
    {
        if (chapters is null)
        {
            return [];
        }

        List<ChapterInfo> validChapters = chapters
            .Where(c => c.StartTime >= TimeSpan.Zero && (naturalDuration <= TimeSpan.Zero || c.StartTime < naturalDuration))
            .OrderBy(c => c.StartTime)
            .ToList();

        List<ChapterCue> sanitized = new(validChapters.Count);
        for (int i = 0; i < validChapters.Count; i++)
        {
            ChapterInfo current = validChapters[i];
            TimeSpan startTime = current.StartTime;
            TimeSpan duration = current.Duration;

            TimeSpan? maxAllowedDuration = i + 1 < validChapters.Count
                ? validChapters[i + 1].StartTime - startTime
                : naturalDuration > TimeSpan.Zero
                    ? naturalDuration - startTime
                    : null;

            if (duration <= TimeSpan.Zero)
            {
                duration = maxAllowedDuration ?? TimeSpan.Zero;
            }
            else if (maxAllowedDuration.HasValue && duration > maxAllowedDuration.Value)
            {
                duration = maxAllowedDuration.Value;
            }

            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            sanitized.Add(new ChapterCue
            {
                Title = current.Title ?? string.Empty,
                Duration = duration,
                StartTime = startTime
            });
        }

        return sanitized;
    }
}
