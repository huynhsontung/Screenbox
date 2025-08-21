using System;
using System.Collections.Generic;
using Screenbox.Core.Models;

namespace Screenbox.Core.Models;

public class PersistentPlaylist
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset Created { get; set; }
    public List<PersistentMediaRecord> Items { get; set; } = new();
}
