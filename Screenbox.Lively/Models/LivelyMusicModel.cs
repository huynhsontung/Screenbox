using System.Collections.Generic;

namespace Screenbox.Lively.Models;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public class LivelyMusicModel
{
    public string AlbumArtist { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public int AlbumTrackCount { get; set; }
    public string Artist { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new();
    public string PlaybackType { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int TrackNumber { get; set; }
}
