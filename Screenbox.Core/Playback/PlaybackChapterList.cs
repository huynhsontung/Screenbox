using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Media.Core;
using LibVLCSharp.Shared.Structures;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackChapterList : ReadOnlyObservableCollection<ChapterCue>
    {
        private readonly ObservableCollection<ChapterCue> _chapters;

        public PlaybackChapterList() : base(new ObservableCollection<ChapterCue>())
        {
            _chapters = (ObservableCollection<ChapterCue>)Items;
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
            foreach (ChapterCue chapterCue in chapterCues)
            {
                _chapters.Add(chapterCue);
            }
        }
    }
}
