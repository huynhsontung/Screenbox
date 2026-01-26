#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.Controllers;

public sealed class LastPositionTracker : ObservableRecipient,
    IRecipient<SuspendingMessage>
{
    private const int Capacity = 64;
    private const string SaveFileName = "last_positions.bin";

    public bool IsLoaded => LastUpdated != default;

    public DateTimeOffset LastUpdated { get; private set; }

    private readonly IFilesService _filesService;
    private List<MediaLastPosition> _lastPositions = new(Capacity + 1);
    private MediaLastPosition? _updateCache;
    private string? _removeCache;

    public LastPositionTracker(IFilesService filesService)
    {
        _filesService = filesService;

        IsActive = true;
    }

    public void Receive(SuspendingMessage message)
    {
        message.Reply(SaveToDiskAsync());
    }

    public void UpdateLastPosition(string location, TimeSpan position)
    {
        LastUpdated = DateTimeOffset.Now;
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
        LastUpdated = DateTimeOffset.Now;
        if (_removeCache == location) return;
        _lastPositions.RemoveAll(x => x.Location == location);
        _removeCache = location;
    }

    public async Task SaveToDiskAsync()
    {
        try
        {
            await _filesService.SaveToDiskAsync(ApplicationData.Current.TemporaryFolder, SaveFileName, _lastPositions);
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
            _lastPositions = lastPositions;
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

    public async Task DeleteFromDiskAsync()
    {
        try
        {
            _lastPositions.Clear();
            _updateCache = null;
            _removeCache = null;
            LastUpdated = default;

            //await _filesService.SaveToDiskAsync(ApplicationData.Current.TemporaryFolder, SaveFileName, _lastPositions);

            var folder = ApplicationData.Current.TemporaryFolder;
            if (await folder.TryGetItemAsync(SaveFileName) is StorageFile file)
            {
                await file.DeleteAsync();
            }
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
