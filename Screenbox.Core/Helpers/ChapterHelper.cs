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

        IEnumerable<ChapterInfo> rawChapters = vlcChapters.Select(c => new ChapterInfo(
            TimeSpan.FromMilliseconds(c.TimeOffset),
            TimeSpan.FromMilliseconds(c.Duration),
            c.Name));

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
            .Where(c => c.StartTime >= TimeSpan.Zero)
            .OrderBy(c => c.StartTime)
            .ToList();

        if (naturalDuration > TimeSpan.Zero)
        {
            validChapters.RemoveAll(c => c.StartTime >= naturalDuration);
        }

        List<ChapterCue> sanitized = new(validChapters.Count);
        for (int i = 0; i < validChapters.Count; i++)
        {
            ChapterInfo current = validChapters[i];
            TimeSpan startTime = current.StartTime;
            TimeSpan duration = current.Duration;

            if (duration <= TimeSpan.Zero)
            {
                if (i + 1 < validChapters.Count)
                {
                    duration = validChapters[i + 1].StartTime - startTime;
                }
                else if (naturalDuration > TimeSpan.Zero)
                {
                    duration = naturalDuration - startTime;
                }
                else
                {
                    duration = TimeSpan.Zero;
                }
            }

            if (i + 1 < validChapters.Count)
            {
                TimeSpan maxAllowedDuration = validChapters[i + 1].StartTime - startTime;
                if (duration > maxAllowedDuration)
                {
                    duration = maxAllowedDuration;
                }
            }
            else if (naturalDuration > TimeSpan.Zero && startTime + duration > naturalDuration)
            {
                duration = naturalDuration - startTime;
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
