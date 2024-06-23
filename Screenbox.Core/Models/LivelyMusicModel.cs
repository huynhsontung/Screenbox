using System.Collections.Generic;

namespace Screenbox.Core.Models;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public class LivelyMusicModel
{
    public string AlbumArtist { get; set; }
    public string AlbumTitle { get; set; }
    public int AlbumTrackCount { get; set; }
    public string Artist { get; set; }
    public List<string> Genres { get; set; }
    public string PlaybackType { get; set; }
    public string Subtitle { get; set; }
    public string Thumbnail { get; set; }
    public string Title { get; set; }
    public int TrackNumber { get; set; }
}
