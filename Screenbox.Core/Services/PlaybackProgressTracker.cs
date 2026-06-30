#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.Sqlite;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

public sealed class PlaybackProgressTracker : ObservableRecipient, IPlaybackProgressTracker,
    IRecipient<SuspendingMessage>
{
    private const int Capacity = 64;

    public bool IsLoaded => LastUpdated != default;

    public DateTimeOffset LastUpdated { get; private set; }

    private readonly IDatabaseService _databaseService;
    private List<MediaPlaybackProgress> _progressList = new(Capacity + 1);
    private MediaPlaybackProgress? _updateCache;
    private string? _removeCache;

    public PlaybackProgressTracker(IDatabaseService databaseService)
    {
        _databaseService = databaseService;

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
            if (item is null)
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

    /// <summary>
    /// Persists the current progress list to the SQLite database.
    /// All existing rows are replaced in a single transaction.
    /// </summary>
    public async Task SaveToDiskAsync()
    {
        // Snapshot the list on the calling thread before going async.
        var snapshot = new List<MediaPlaybackProgress>(_progressList);
        try
        {
            await Task.Run(() => WriteProgressToDatabase(snapshot));
        }
        catch (Exception e)
        {
            LogService.Log($"Failed to save playback progress\n{e}");
        }
    }

    /// <summary>
    /// Loads the persisted progress list from the SQLite database.
    /// </summary>
    public async Task LoadFromDiskAsync()
    {
        try
        {
            List<MediaPlaybackProgress> loaded = await Task.Run(ReadProgressFromDatabase);
            loaded.Capacity = Capacity;
            _progressList = loaded;
            LastUpdated = DateTimeOffset.UtcNow;
        }
        catch (Exception e)
        {
            // Non-fatal: app starts with empty progress list.
            LogService.Log($"Failed to load playback progress\n{e}");
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void WriteProgressToDatabase(List<MediaPlaybackProgress> snapshot)
    {
        using var connection = _databaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();

        // Full replace: delete all rows then re-insert the current snapshot.
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM playback_progress;";
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO playback_progress (location, position_ticks) VALUES (@loc, @ticks);";
            var pLoc = cmd.Parameters.Add("@loc", SqliteType.Text);
            var pTicks = cmd.Parameters.Add("@ticks", SqliteType.Integer);

            foreach (MediaPlaybackProgress item in snapshot)
            {
                pLoc.Value = item.Location;
                pTicks.Value = item.Position.Ticks;
                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    private List<MediaPlaybackProgress> ReadProgressFromDatabase()
    {
        using var connection = _databaseService.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT location, position_ticks FROM playback_progress;";

        var result = new List<MediaPlaybackProgress>(Capacity);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string location = reader.GetString(0);
            long ticks = reader.GetInt64(1);
            result.Add(new MediaPlaybackProgress(location, new TimeSpan(ticks)));
        }

        return result;
    }
}

