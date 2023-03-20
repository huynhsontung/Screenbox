namespace Screenbox.Core.Messages
{
    public sealed record OverrideControlsHideMessage(int Delay)
    {
        public int Delay { get; } = Delay;
    }
}
