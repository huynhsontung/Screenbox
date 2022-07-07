#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.Media.Core;

namespace Screenbox.Core.Playback
{
    public class ObservableTrackList<T> : IReadOnlyList<T>, ISingleSelectMediaTrackList, INotifyCollectionChanged
    {
        public event TypedEventHandler<ISingleSelectMediaTrackList, object?>? SelectedIndexChanged;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public T this[int index] => TrackList[index];

        public int Count => TrackList.Count;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, null);
            }
        }

        protected ObservableCollection<T> TrackList { get; }

        private int _selectedIndex;

        public ObservableTrackList()
        {
            TrackList = new ObservableCollection<T>();
            TrackList.CollectionChanged += TrackList_CollectionChanged;
        }

        private void TrackList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
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
