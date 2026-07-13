using Screenbox.Lively.Enums;

namespace Screenbox.Lively.Models;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public record LivelyInfoModel
{
    public string AppVersion { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public LivelyWallpaperType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public bool IsAbsolutePath { get; set; }
    public string Id { get; set; } = string.Empty;
    // public List<string> Tags { get; set; }
    public int Version { get; set; }
}
