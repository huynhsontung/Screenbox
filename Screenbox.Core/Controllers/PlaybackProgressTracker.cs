#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Data;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;

namespace Screenbox.Core.Controllers;

/// <summary>
/// Tracks and persists the last playback position for each media item.
/// Positions are kept in memory and lazily flushed to the SQLite database on app suspend.
/// </summary>
public sealed class PlaybackProgressTracker : ObservableRecipient,
    IRecipient<SuspendingMessage>
{
    private const int Capacity = 64;

    public bool IsLoaded => LastUpdated != default;

    public DateTimeOffset LastUpdated { get; private set; }

    private readonly IScreenboxDatabase _database;
    private List<MediaPlaybackProgress> _progresses = new(Capacity + 1);
    private MediaPlaybackProgress? _updateCache;
    private string? _removeCache;

    public PlaybackProgressTracker(IScreenboxDatabase database)
    {
        _database = database;

        IsActive = true;
    }

    public void Receive(SuspendingMessage message)
    {
        message.Reply(SaveToDbAsync());
    }

    public void UpdatePlaybackProgress(string location, TimeSpan position)
    {
        LastUpdated = DateTimeOffset.Now;
        _removeCache = null;
        MediaPlaybackProgress? item = _updateCache;
        if (item?.Location == location)
        {
            item.Position = position;
            if (_progresses.FirstOrDefault() != item)
            {
                int index = _progresses.IndexOf(item);
                if (index >= 0)
                {
                    _progresses.RemoveAt(index);
                }

                _progresses.Insert(0, item);
            }
        }
        else
        {
            item = _progresses.Find(x => x.Location == location);
            if (item == null)
            {
                item = new MediaPlaybackProgress(location, position);
                _progresses.Insert(0, item);
                if (_progresses.Count > Capacity)
                {
                    _progresses.RemoveAt(Capacity);
                }
            }
            else
            {
                item.Position = position;
            }
        }

        _updateCache = item;
    }

    public TimeSpan GetPlaybackProgress(string location)
    {
        return _progresses.Find(x => x.Location == location)?.Position ?? TimeSpan.Zero;
    }

    public void RemovePlaybackProgress(string location)
    {
        LastUpdated = DateTimeOffset.Now;
        if (_removeCache == location) return;
        _progresses.RemoveAll(x => x.Location == location);
        _removeCache = location;
    }

    public void ClearAll()
    {
        LastUpdated = DateTimeOffset.Now;
        _progresses.Clear();
        _updateCache = null;
        _removeCache = null;
    }

    public async Task SaveToDbAsync()
    {
        try
        {
            var entities = _progresses
                .Take(Capacity)
                .Select((p, i) => new PlaybackProgressEntity
                {
                    Location = p.Location,
                    PositionTicks = p.Position.Ticks,
                    SortOrder = i
                });
            await _database.SaveAllPlaybackProgressesAsync(entities);
        }
        catch (Exception)
        {
            // DB errors are non-fatal; data will be recollected on next session
        }
    }

    public async Task LoadFromDbAsync()
    {
        try
        {
            List<PlaybackProgressEntity> entities = await _database.GetAllPlaybackProgressesAsync();
            _progresses = entities
                .Select(e => new MediaPlaybackProgress(e.Location, TimeSpan.FromTicks(e.PositionTicks)))
                .ToList();
            _progresses.Capacity = Capacity + 1;
        }
        catch (Exception)
        {
            // Data loss is handled gracefully: start with an empty progress list
        }
        finally
        {
            // Mark as loaded regardless of outcome so we do not retry on every player change
            LastUpdated = DateTimeOffset.UtcNow;
        }
    }
}
