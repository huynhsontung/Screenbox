#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace Screenbox.Core
{
    internal class LastPositionTracker
    {
        private const int Capacity = 64;
        private const string SaveFileName = "last_positions.bin";

        private List<MediaLastPosition> _lastPositions;
        private MediaLastPosition? _updateCache;
        private string? _removeCache;

        public LastPositionTracker()
        {
            _lastPositions = new List<MediaLastPosition>(Capacity + 1);
        }

        public void UpdateLastPosition(string location, TimeSpan position)
        {
            _removeCache = null;
            MediaLastPosition? item = _updateCache;
            if (item?.Location == location)
            {
                item.Position = position;
                if (_lastPositions.FirstOrDefault() != item)
                {
                    int index = _lastPositions.IndexOf(item);
                    if (index >= 0)
                    {
                        _lastPositions.RemoveAt(index);
                        _lastPositions.Insert(0, item);
                    }
                }
            }
            else
            {
                item = _lastPositions.Find(x => x.Location == location);
                if (item == null)
                {
                    item = new MediaLastPosition(location, position);
                    _lastPositions.Insert(0, item);
                    if (_lastPositions.Count > Capacity)
                    {
                        _lastPositions.RemoveAt(Capacity);
                    }
                }
                else
                {
                    item.Position = position;
                }
            }

            _updateCache = item;
        }

        public TimeSpan GetPosition(string location)
        {
            return _lastPositions.Find(x => x.Location == location)?.Position ?? TimeSpan.Zero;
        }

        public void RemovePosition(string location)
        {
            if (_removeCache == location) return;
            _lastPositions.RemoveAll(x => x.Location == location);
            _removeCache = location;
        }

        public async Task SaveToDiskAsync()
        {
            StorageFile file =
                await ApplicationData.Current.TemporaryFolder.CreateFileAsync(SaveFileName,
                    CreationCollisionOption.OpenIfExists);
            using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using Stream writeStream = stream.AsStreamForWrite();
            Serializer.Serialize(writeStream, _lastPositions);
        }

        public async Task LoadFromDiskAsync()
        {
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            IStorageItem? item = await tempFolder.TryGetItemAsync(SaveFileName);
            if (item is StorageFile file)
            {
                try
                {
                    using IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();
                    using Stream readStream = stream.AsStreamForRead();

                    List<MediaLastPosition>? lastPositions = Serializer.Deserialize<List<MediaLastPosition>>(readStream);
                    if (lastPositions != null)
                    {
                        lastPositions.Capacity = Capacity;
                        _lastPositions = lastPositions;
                    }
                }
                catch (Exception)
                {
                    // pass
                }
            }
        }
    }
}
