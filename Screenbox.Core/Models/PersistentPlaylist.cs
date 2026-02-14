using System;
using System.Collections.Generic;

namespace Screenbox.Core.Models;

public class PersistentPlaylist
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; }
    public List<PersistentMediaRecord> Items { get; set; } = new();
}
