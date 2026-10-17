using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LibVLCSharp.Shared.Structures;
using Windows.Media.Core;

namespace Screenbox.Core.Playback;

public sealed partial class PlaybackChapterList : ReadOnlyCollection<ChapterCue>
{
    private readonly List<ChapterCue> _chapters;
    private readonly PlaybackItem? _item;

    internal PlaybackChapterList() : base(new List<ChapterCue>())
    {
        _chapters = (List<ChapterCue>)Items;
    }

    internal PlaybackChapterList(PlaybackItem item) : this()
    {
        _item = item;
    }

    public void Load(IMediaPlayer player)
    {
        if (player is not VlcMediaPlayer vlcPlayer || player.PlaybackItem != _item)
        {
            return;
        }

        if (vlcPlayer.VlcPlayer.ChapterCount > 0)
        {
            List<ChapterDescription> chapterDescriptions = new();
            for (int i = 0; i < vlcPlayer.VlcPlayer.TitleCount; i++)
            {
                ChapterDescription[]? descriptions = vlcPlayer.VlcPlayer.FullChapterDescriptions(i);
                if (descriptions is not null)
                {
                    chapterDescriptions.AddRange(descriptions);
                }
            }

            Load(chapterDescriptions, player.NaturalDuration);
        }
        else
        {
            ChapterDescription[]? descriptions = vlcPlayer.VlcPlayer.FullChapterDescriptions();
            if (descriptions is not null)
            {
                Load(descriptions, player.NaturalDuration);
            }
        }

        vlcPlayer.Chapter = _chapters.FirstOrDefault();
    }

    internal void Load(IEnumerable<ChapterDescription>? vlcChapters, TimeSpan naturalDuration)
    {
        _chapters.Clear();
        if (vlcChapters is null)
        {
            return;
        }

        List<ChapterDescription> validChapters = vlcChapters
            .Where(c => c.TimeOffset >= 0)
            .OrderBy(c => c.TimeOffset)
            .ToList();

        if (naturalDuration > TimeSpan.Zero)
        {
            validChapters.RemoveAll(c => c.TimeOffset >= naturalDuration.TotalMilliseconds);
        }

        List<ChapterCue> chapterCues = new(validChapters.Count);
        for (int i = 0; i < validChapters.Count; i++)
        {
            ChapterDescription c = validChapters[i];
            TimeSpan startTime = TimeSpan.FromMilliseconds(c.TimeOffset);
            TimeSpan duration = TimeSpan.FromMilliseconds(c.Duration);

            if (duration <= TimeSpan.Zero)
            {
                if (i + 1 < validChapters.Count)
                {
                    duration = TimeSpan.FromMilliseconds(validChapters[i + 1].TimeOffset - c.TimeOffset);
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
                TimeSpan maxAllowedDuration = TimeSpan.FromMilliseconds(validChapters[i + 1].TimeOffset - c.TimeOffset);
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

            chapterCues.Add(new ChapterCue
            {
                Title = c.Name ?? string.Empty,
                Duration = duration,
                StartTime = startTime
            });
        }

        _chapters.AddRange(chapterCues);
    }
}
