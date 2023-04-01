namespace Screenbox.Core.Messages
{
    public sealed class ShowPlayPauseBadgeMessage
    {
        public bool IsPlaying { get; }

        public ShowPlayPauseBadgeMessage(bool isPlaying)
        {
            IsPlaying = isPlaying;
        }
    }
}
