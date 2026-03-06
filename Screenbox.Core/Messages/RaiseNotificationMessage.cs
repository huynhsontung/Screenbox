using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages;

/// <summary>
/// A message to raise a general notification with a title, message, and severity level.
/// Used as a replacement for the event-based notification pattern.
/// </summary>
public class RaiseNotificationMessage
{
    public NotificationLevel Level { get; }

    public string Title { get; }

    public string Message { get; }

    public RaiseNotificationMessage(NotificationLevel level, string title, string message)
    {
        Level = level;
        Title = title;
        Message = message;
    }
}
