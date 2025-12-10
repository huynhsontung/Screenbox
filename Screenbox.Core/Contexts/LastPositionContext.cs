#nullable enable

using System;
using System.Collections.Generic;
using Screenbox.Core.Models;

namespace Screenbox.Core.Contexts;

public sealed class LastPositionContext
{
    public DateTimeOffset LastUpdated { get; set; }
    public List<MediaLastPosition> LastPositions { get; set; } = new(65);
    public MediaLastPosition? UpdateCache { get; set; }
    public string? RemoveCache { get; set; }
}
