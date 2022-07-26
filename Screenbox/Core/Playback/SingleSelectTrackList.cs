#nullable enable

using System.Collections;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Media.Core;

namespace Screenbox.Core.Playback
{
    public class SingleSelectTrackList<T> : IReadOnlyList<T>, ISingleSelectMediaTrackList
    {
        public event TypedEventHandler<ISingleSelectMediaTrackList, object?>? SelectedIndexChanged;

        public T this[int index] => TrackList[index];

        public int Count => TrackList.Count;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value == _selectedIndex) return;
                _selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, null);
            }
        }

        protected List<T> TrackList { get; }

        private int _selectedIndex;

        public SingleSelectTrackList()
        {
            TrackList = new List<T>();
            _selectedIndex = -1;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return TrackList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return TrackList.GetEnumerator();
        }
    }
}
