using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Core;
using LibVLCSharp.Shared.Structures;

namespace Screenbox.Core.Playback
{
    public class PlaybackChapterList : IReadOnlyList<ChapterCue>
    {
        public int Count => _chapters.Count;

        public ChapterCue this[int index] => _chapters[index];

        private readonly List<ChapterCue> _chapters;

        public PlaybackChapterList()
        {
            _chapters = new List<ChapterCue>();
        }

        internal void Load(IEnumerable<ChapterDescription> vlcChapters)
        {
            IEnumerable<ChapterCue> chapterCues = vlcChapters.Select(c => new ChapterCue
            {
                Title = c.Name,
                Duration = TimeSpan.FromMilliseconds(c.Duration),
                StartTime = TimeSpan.FromMilliseconds(c.TimeOffset)
            });

            _chapters.Clear();
            _chapters.AddRange(chapterCues);
        }

        public IEnumerator<ChapterCue> GetEnumerator()
        {
            return _chapters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _chapters.GetEnumerator();
        }
    }
}
