#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

public sealed partial class DatabaseService
{
    /// <inheritdoc/>
    public async Task ReplacePlaybackProgressAsync(IReadOnlyList<MediaPlaybackProgress> snapshot)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, "DELETE FROM playback_progress;");

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

    /// <inheritdoc/>
    public async Task<List<MediaPlaybackProgress>> LoadPlaybackProgressAsync()
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT location, position_ticks FROM playback_progress;";

        var result = new List<MediaPlaybackProgress>();
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
