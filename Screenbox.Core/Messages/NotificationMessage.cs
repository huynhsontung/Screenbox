using System.Windows.Input;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages;

public sealed class NotificationMessage
{
    public NotificationLevel Level { get; init; }

    public NotificationKind Kind { get; init; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public double? NumericValue { get; set; }

    public string? ActionContent { get; set; }

    public ICommand? ActionCommand { get; set; }

    public NotificationMessage(
        NotificationLevel level,
        NotificationKind kind = NotificationKind.None,
        string? title = null,
        string? message = null,
        double? numericValue = null,
        string? actionContent = null,
        ICommand? actionCommand = null)
    {
        Level = level;
        Kind = kind;
        Title = title;
        Message = message;
        NumericValue = numericValue;
        ActionContent = actionContent;
        ActionCommand = actionCommand;
    }
}
