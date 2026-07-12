using System;

namespace Screenbox.Lively.Models;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public record LivelyWallpaperModel
{
    public LivelyInfoModel Model { get; set; } = new LivelyInfoModel();
    public string Path { get; set; } = string.Empty;
    public string PreviewPath { get; set; } = string.Empty;
    public bool IsPreset { get; set; }
    public bool IsMusic { get; set; }
    public bool IsAudio { get; set; }
    public bool IsPauseNotify { get; set; }
    public Uri? AuthorUrl { get; set; }
}
