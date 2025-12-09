#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
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
    public sealed class LastPositionTracker : ObservableRecipient,
        IRecipient<SuspendingMessage>
    {
        private const int Capacity = 64;
        private const string SaveFileName = "last_positions.bin";

        public bool IsLoaded => State.LastUpdated != default;

        public DateTimeOffset LastUpdated
        {
            get => State.LastUpdated;
            private set => State.LastUpdated = value;
        }

        private readonly IFilesService _filesService;
        private readonly SessionContext _sessionContext;
        private LastPositionState State => _sessionContext.LastPositions;

        public LastPositionTracker(IFilesService filesService, SessionContext sessionContext)
        {
            _filesService = filesService;
            _sessionContext = sessionContext;

            IsActive = true;
        }

        public void Receive(SuspendingMessage message)
        {
            message.Reply(SaveToDiskAsync());
        }

        public void UpdateLastPosition(string location, TimeSpan position)
        {
            LastUpdated = DateTimeOffset.Now;
            State.RemoveCache = null;
            MediaLastPosition? item = State.UpdateCache;
            if (item?.Location == location)
            {
                item.Position = position;
                if (State.LastPositions.FirstOrDefault() != item)
                {
                    int index = State.LastPositions.IndexOf(item);
                    if (index >= 0)
                    {
                        State.LastPositions.RemoveAt(index);
                    }

                    State.LastPositions.Insert(0, item);
                }
            }
            else
            {
                item = State.LastPositions.Find(x => x.Location == location);
                if (item == null)
                {
                    item = new MediaLastPosition(location, position);
                    State.LastPositions.Insert(0, item);
                    if (State.LastPositions.Count > Capacity)
                    {
                        State.LastPositions.RemoveAt(Capacity);
                    }
                }
                else
                {
                    item.Position = position;
                }
            }

            State.UpdateCache = item;
        }

        public TimeSpan GetPosition(string location)
        {
            return State.LastPositions.Find(x => x.Location == location)?.Position ?? TimeSpan.Zero;
        }

        public void RemovePosition(string location)
        {
            LastUpdated = DateTimeOffset.Now;
            if (State.RemoveCache == location) return;
            State.LastPositions.RemoveAll(x => x.Location == location);
            State.RemoveCache = location;
        }

        public async Task SaveToDiskAsync()
        {
            try
            {
                await _filesService.SaveToDiskAsync(ApplicationData.Current.TemporaryFolder, SaveFileName, State.LastPositions.ToList());
            }
            catch (FileLoadException)
            {
                // File in use. Skipped
            }
        }

        public async Task LoadFromDiskAsync()
        {
            try
            {
                List<MediaLastPosition> lastPositions =
                    await _filesService.LoadFromDiskAsync<List<MediaLastPosition>>(ApplicationData.Current.TemporaryFolder, SaveFileName);
                lastPositions.Capacity = Capacity;
                State.LastPositions = lastPositions;
                LastUpdated = DateTimeOffset.UtcNow;
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
