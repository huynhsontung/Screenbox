#nullable enable

using System;
using System.Collections.Generic;
using Screenbox.Core.Models;

namespace Screenbox.Core.Contexts;

internal sealed class LastPositionContext
{
    internal DateTimeOffset LastUpdated { get; set; }
    internal List<MediaLastPosition> LastPositions { get; set; } = new(65);
    internal MediaLastPosition? UpdateCache { get; set; }
    internal string? RemoveCache { get; set; }
}
