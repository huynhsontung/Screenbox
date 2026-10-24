using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LibVLCSharp.Shared.Structures;
using Screenbox.Core.Helpers;
using Windows.Media.Core;

namespace Screenbox.Core.Playback;

public sealed partial class VlcChapterList : ReadOnlyCollection<ChapterCue>
{
    private readonly List<ChapterCue> _chapters;
    private readonly IPlaybackItem _item;

    internal VlcChapterList(IPlaybackItem item) : base(new List<ChapterCue>())
    {
        _item = item;
        _chapters = (List<ChapterCue>)Items;
    }

    public void Load(VlcMediaPlayer vlcPlayer)
    {
        if (vlcPlayer.PlaybackItem != _item)
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

            Load(chapterDescriptions, vlcPlayer.NaturalDuration);
        }
        else
        {
            ChapterDescription[]? descriptions = vlcPlayer.VlcPlayer.FullChapterDescriptions();
            if (descriptions is not null)
            {
                Load(descriptions, vlcPlayer.NaturalDuration);
            }
        }

        vlcPlayer.Chapter = _chapters.FirstOrDefault();
    }

    private void Load(IEnumerable<ChapterDescription> vlcChapters, TimeSpan naturalDuration)
    {
        List<ChapterCue> chapterCues = ChapterHelper.FromVlcDescriptions(vlcChapters, naturalDuration);
        _chapters.Clear();
        _chapters.AddRange(chapterCues);
    }
}
