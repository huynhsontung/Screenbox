using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;
using Screenbox.Services;
using System.IO;
using ProtoBuf;

namespace Screenbox.Core
{
    internal class LastPositionTracker
    {
        private const int Capacity = 100;

        private readonly IFilesService _filesService;
        private List<MediaLastPosition> _lastPositions;
        private MediaLastPosition? _cache;

        public LastPositionTracker(IFilesService filesService)
        {
            _filesService = filesService;
            _lastPositions = new List<MediaLastPosition>(Capacity + 1);
        }

        public void UpdateLastPosition(string location, TimeSpan position)
        {
            MediaLastPosition? item = _cache;
            if (item?.Location == location)
            {
                item.Position = position;
                int index = _lastPositions.IndexOf(item);
                if (index > 0)
                {
                    _lastPositions.RemoveAt(index);
                    _lastPositions.Insert(0, item);
                }
            }
            else
            {
                item = _lastPositions.Find(x => x.Location == location);
                if (item == null)
                {
                    _cache = item = new MediaLastPosition(location, position);
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
        }

        public TimeSpan GetPosition(string location)
        {
            return _cache?.Location == location
                ? _cache.Position
                : _lastPositions.Find(x => x.Location == location)?.Position ?? TimeSpan.Zero;
        }

        public async Task SaveToDiskAsync()
        {
            StorageFile file =
                await _filesService.GetFileAsync(ApplicationData.Current.TemporaryFolder, "last_positions.bin");
            using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using Stream writeStream = stream.AsStreamForWrite();
            Serializer.Serialize(writeStream, _lastPositions);
        }

        public async Task LoadFromDiskAsync()
        {
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            IStorageItem? item = await tempFolder.TryGetItemAsync("last_positions.bin");
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
