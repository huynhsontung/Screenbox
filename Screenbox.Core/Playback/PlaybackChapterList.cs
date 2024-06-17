#nullable enable

using LibVLCSharp.Shared.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackChapterList : ReadOnlyCollection<ChapterCue>
    {
        private readonly List<ChapterCue> _chapters;
        private readonly IPlaybackItem _item;
        public TimedMetadataTrack? ChapterTrack { get; }

        internal PlaybackChapterList(IPlaybackItem item) : base(new List<ChapterCue>())
        {
            _item = item;
            _chapters = (List<ChapterCue>)Items;
        }

        internal PlaybackChapterList(MediaPlaybackTimedMetadataTrackList source, IPlaybackItem item) : base(new List<ChapterCue>())
        {
            _item = item;
            _chapters = (List<ChapterCue>)Items;

            foreach (TimedMetadataTrack metadataTrack in source)
            {
                if (metadataTrack.TimedMetadataKind is not TimedMetadataKind.Chapter) continue;
                ChapterTrack = metadataTrack;
                foreach (IMediaCue cue in metadataTrack.Cues)
                {
                    if (cue is ChapterCue chapterCue)
                        _chapters.Add(chapterCue);
                    else
                        _chapters.Add(new ChapterCue
                        {
                            Title = string.Empty,
                            Id = cue.Id,
                            Duration = cue.Duration,
                            StartTime = cue.StartTime
                        });
                }

                break;
            }
        }

        public void Load(IMediaPlayer player)
        {
            if (player is not VlcMediaPlayer vlcPlayer || player.PlaybackItem != _item)
                return;

            if (vlcPlayer.VlcPlayer.ChapterCount > 0)
            {
                List<ChapterDescription> chapterDescriptions = new();
                for (int i = 0; i < vlcPlayer.VlcPlayer.TitleCount; i++)
                {
                    chapterDescriptions.AddRange(vlcPlayer.VlcPlayer.FullChapterDescriptions(i));
                }

                Load(chapterDescriptions);
            }
            else
            {
                Load(vlcPlayer.VlcPlayer.FullChapterDescriptions());
            }

            vlcPlayer.Chapter = _chapters.FirstOrDefault();
        }

        private void Load(IEnumerable<ChapterDescription> vlcChapters)
        {
            IEnumerable<ChapterCue> chapterCues = vlcChapters.Select(c => new ChapterCue
            {
                Title = c.Name ?? string.Empty,
                Duration = TimeSpan.FromMilliseconds(c.Duration),
                StartTime = TimeSpan.FromMilliseconds(c.TimeOffset)
            });

            _chapters.Clear();
            _chapters.AddRange(chapterCues);
        }
    }
}
