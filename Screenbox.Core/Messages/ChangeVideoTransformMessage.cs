using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent when the active video orientation transform (rotation and/or flipping) changes.
/// </summary>
public sealed class ChangeVideoTransformMessage : ValueChangedMessage<VideoTransform>
{
    public ChangeVideoTransformMessage(VideoTransform value) : base(value)
    {
    }
}
