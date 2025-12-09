#nullable enable

using System;

namespace Screenbox.Core.Contexts;

internal sealed class TransportControlsState
{
    internal DateTime LastUpdated { get; set; } = DateTime.MinValue;
}
