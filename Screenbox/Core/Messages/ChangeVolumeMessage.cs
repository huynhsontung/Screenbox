namespace Screenbox.Core.Messages
{
    public class ChangeVolumeMessage
    {
        public double Volume { get; }

        public bool IsOffset { get; }

        public ChangeVolumeMessage(double volume, bool isOffset = false)
        {
            Volume = volume;
            IsOffset = isOffset;
        }
    }
}
