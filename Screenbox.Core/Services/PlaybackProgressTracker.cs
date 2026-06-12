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
using Windows.Storage;

namespace Screenbox.Core.Services;

public sealed class PlaybackProgressTracker : ObservableRecipient, IPlaybackProgressTracker,
    IRecipient<SuspendingMessage>
{
    private const int Capacity = 64;
    private const string SaveFileName = "last_positions.bin";

    public bool IsLoaded => LastUpdated != default;

    public DateTimeOffset LastUpdated { get; private set; }

    private readonly IFilesService _filesService;
    private List<MediaPlaybackProgress> _progressList = new(Capacity + 1);
    private MediaPlaybackProgress? _updateCache;
    private string? _removeCache;

    public PlaybackProgressTracker(IFilesService filesService)
    {
        _filesService = filesService;

        IsActive = true;
    }

    public void Receive(SuspendingMessage message)
    {
        message.Reply(SaveToDiskAsync());
    }

    public void UpdateProgress(string location, TimeSpan position)
    {
        LastUpdated = DateTimeOffset.Now;
        _removeCache = null;
        MediaPlaybackProgress? item = _updateCache;
        if (item?.Location == location)
        {
            item.Position = position;
            if (_progressList.FirstOrDefault() != item)
            {
                int index = _progressList.IndexOf(item);
                if (index >= 0)
                {
                    _progressList.RemoveAt(index);
                }

                _progressList.Insert(0, item);
            }
        }
        else
        {
            item = _progressList.Find(x => x.Location == location);
            if (item == null)
            {
                item = new MediaPlaybackProgress(location, position);
                _progressList.Insert(0, item);
                if (_progressList.Count > Capacity)
                {
                    _progressList.RemoveAt(Capacity);
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
        return _progressList.Find(x => x.Location == location)?.Position ?? TimeSpan.Zero;
    }

    public void RemovePosition(string location)
    {
        LastUpdated = DateTimeOffset.Now;
        if (_removeCache == location) return;
        _progressList.RemoveAll(x => x.Location == location);
        _removeCache = location;
    }

    public void ClearAll()
    {
        LastUpdated = DateTimeOffset.Now;
        _progressList.Clear();
        _updateCache = null;
        _removeCache = null;
    }

    public async Task SaveToDiskAsync()
    {
        try
        {
            await _filesService.SaveToDiskAsync(ApplicationData.Current.TemporaryFolder, SaveFileName, _progressList);
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
            List<MediaPlaybackProgress> progressList =
                await _filesService.LoadFromDiskAsync<List<MediaPlaybackProgress>>(ApplicationData.Current.TemporaryFolder, SaveFileName);
            progressList.Capacity = Capacity;
            _progressList = progressList;
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
