using CommunityToolkit.Mvvm.Messaging.Messages;
using Windows.Foundation;

namespace Screenbox.Core.Messages
{
    public sealed class ChangeAspectRatioMessage : ValueChangedMessage<Size>
    {
        public ChangeAspectRatioMessage(Size value) : base(value)
        {
        }
    }
}
