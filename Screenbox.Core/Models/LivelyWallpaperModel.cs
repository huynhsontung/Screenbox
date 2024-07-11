using System;

namespace Screenbox.Core.Models;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public class LivelyWallpaperModel
{
    public LivelyInfoModel Model { get; set; }
    public string Path { get; set; }
    public string PreviewPath { get; set; }
    public bool IsPreset { get; set; }
    public bool IsMusic { get; set; }
    public bool IsAudio { get; set; }
    public bool IsPauseNotify { get; set; }
    public Uri AuthorUrl { get; set; }
}
