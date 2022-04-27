namespace Screenbox.Core.Messages
{
    internal class TimeRequestMessage : ChangeValueRequestMessage<double>
    {
        public TimeRequestMessage()
        {
        }

        public TimeRequestMessage(double value) : base(value)
        {
        }
    }
}
