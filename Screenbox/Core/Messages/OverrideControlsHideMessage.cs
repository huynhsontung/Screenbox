namespace Screenbox.Core.Messages
{
    internal sealed record OverrideControlsHideMessage(int Delay)
    {
        public int Delay { get; } = Delay;
    }
}
