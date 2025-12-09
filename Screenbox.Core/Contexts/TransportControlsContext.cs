#nullable enable

using System;

namespace Screenbox.Core.Contexts;

internal sealed class TransportControlsContext
{
    internal DateTime LastUpdated { get; set; } = DateTime.MinValue;
}
