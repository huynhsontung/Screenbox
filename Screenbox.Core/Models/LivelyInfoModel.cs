using Screenbox.Core.Enums;
using System.Collections.Generic;

namespace Screenbox.Core.Models;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public class LivelyInfoModel
{
    public string AppVersion { get; set; }
    public string Title { get; set; }
    public string Thumbnail { get; set; }
    public string Preview { get; set; }
    public string Desc { get; set; }
    public string Author { get; set; }
    public string License { get; set; }
    public string Contact { get; set; }
    public LivelyWallpaperType Type { get; set; }
    public string FileName { get; set; }
    public string Arguments { get; set; }
    public bool IsAbsolutePath { get; set; }
    public string Id { get; set; }
    public List<string> Tags { get; set; }
    public int Version { get; set; }
}
