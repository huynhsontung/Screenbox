using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages;

public sealed class TrackNavigationMessage
{
    public TrackNavigationDirection Direction { get; }

    public TrackNavigationMessage(TrackNavigationDirection direction)
    {
        Direction = direction;
    }
}
