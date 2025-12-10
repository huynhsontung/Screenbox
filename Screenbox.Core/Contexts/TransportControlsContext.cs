#nullable enable

using System;

namespace Screenbox.Core.Contexts;

public sealed class TransportControlsContext
{
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;
}
