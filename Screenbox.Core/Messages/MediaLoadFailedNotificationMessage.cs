namespace Screenbox.Core.Messages
{
    public sealed class MediaLoadFailedNotificationMessage
    {
        public string Reason { get; }

        public string Path { get; }

        public MediaLoadFailedNotificationMessage(string reason, string path)
        {
            Reason = reason;
            Path = path;
        }
    }
}
