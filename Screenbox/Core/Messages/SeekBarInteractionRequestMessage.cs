namespace Screenbox.Core.Messages
{
    internal class SeekBarInteractionRequestMessage : ChangeValueRequestMessage<bool>
    {
        public SeekBarInteractionRequestMessage()
        {
        }

        public SeekBarInteractionRequestMessage(bool value) : base(value)
        {
        }
    }
}
