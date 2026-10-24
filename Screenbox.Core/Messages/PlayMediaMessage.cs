using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Messages;

public sealed class PlayMediaMessage : ValueChangedMessage<MediaViewModel>
{
    public bool Existing { get; }

    public PlayMediaMessage(MediaViewModel value, bool existing = false) : base(value)
    {
        Existing = existing;
    }
}
