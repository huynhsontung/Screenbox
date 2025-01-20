#nullable enable

using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Core.Helpers
{
    internal class LastPositionTracker
    {
        private const int Capacity = 64;
        private const string SaveFileName = "last_positions.bin";

        private List<MediaLastPosition> _lastPositions = new(Capacity + 1);
        private MediaLastPosition? _updateCache;
        private string? _removeCache;

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
                    }

                    _lastPositions.Insert(0, item);
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

        public async Task SaveToDiskAsync(IFilesService filesService)
        {
            try
            {
                await filesService.SaveToDiskAsync(ApplicationData.Current.TemporaryFolder, SaveFileName, _lastPositions.ToList());
            }
            catch (FileLoadException)
            {
                // File in use. Skipped
            }
        }

        public async Task LoadFromDiskAsync(IFilesService filesService)
        {
            try
            {
                List<MediaLastPosition> lastPositions =
                    await filesService.LoadFromDiskAsync<List<MediaLastPosition>>(ApplicationData.Current.TemporaryFolder, SaveFileName);
                lastPositions.Capacity = Capacity;
                _lastPositions = lastPositions;
            }
            catch (FileNotFoundException)
            {
                // pass
            }
            catch (Exception)
            {
                // pass
            }
        }
    }
}
