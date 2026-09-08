using System;
using System.Windows.Input;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages;

/// <summary>
/// Represents a notification payload sent through the application-wide notification system.
/// </summary>
public sealed class NotificationMessage
{
    /// <summary>
    /// Gets the severity level of the notification.
    /// </summary>
    /// <value>
    /// A value of the enumeration that specifies the severity level of the notification.
    /// The default is <see cref="NotificationLevel.Info"/>.
    /// </value>
    public NotificationLevel Level { get; init; }

    /// <summary>
    /// Gets the kind of notification being displayed.
    /// </summary>
    /// <value>
    /// A value of the enumeration that specifies the kind of notification.
    /// The default is <see cref="NotificationKind.None"/>.
    /// </value>
    public NotificationKind Kind { get; init; }

    /// <summary>
    /// Gets or sets the title of the notification.
    /// </summary>
    /// <value>The title of the notification. The default is <see langword="null"/>.</value>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the message of the notification.
    /// </summary>
    /// <value>The message of the notification. The default is <see langword="null"/>.</value>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the numeric value associated with the notification,
    /// which can be used for progress indicators or other purposes.
    /// </summary>
    /// <value>The numeric value associated with the notification. The default is <see langword="null"/>.</value>
    public double? NumericValue { get; set; }

    /// <summary>
    /// Gets or sets the content for the notification action button.
    /// </summary>
    /// <value>The content for the notification action button. The default is <see langword="null"/>.</value>
    public string? ActionContent { get; set; }

    /// <summary>
    /// Gets or sets the command to invoke when the action button is tapped
    /// in the notification.
    /// </summary>
    /// <value>
    /// The command to invoke when the action button is tapped in the notification.
    /// The default is <see langword="null"/>.
    /// </value>
    public ICommand? ActionCommand { get; set; }

    ///// <summary>
    ///// Gets or sets the parameter to pass to the command for the action button
    ///// in the notification.
    ///// </summary>
    ///// <value>
    ///// The parameter to pass to the command for the action button in the notification.
    ///// The default is <see langword="null"/>.
    ///// </value>
    //public object? ActionCommandParameter { get; set; }

    /// <summary>
    /// Gets or sets the duration override for the notification display.
    /// </summary>
    /// <value>The duration override for the notification display. The default is <see langword="null"/>.</value>
    public TimeSpan? DurationOverride { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationMessage"/> class
    /// with the specified notification data.
    /// </summary>
    /// <param name="level">A value of the enumeration that specifies the severity level of the notification.</param>
    /// <param name="kind">A value of the enumeration that specifies the kind of notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message of the notification.</param>
    /// <param name="numericValue">The numeric value associated with the notification.</param>
    /// <param name="actionContent">The content displayed for the notification action.</param>
    /// <param name="actionCommand">The command to invoke when the action button is tapped.</param>
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

    //public NotificationMessage SetTitle(string title)
    //{
    //    Title = title;
    //    return this;
    //}

    //public NotificationMessage SetMessage(string message)
    //{
    //    Message = message;
    //    return this;
    //}

    //public NotificationMessage SetValue(double value)
    //{
    //    NumericValue = value;
    //    return this;
    //}

    //public NotificationMessage SetAction(string? content, ICommand? command, object? commandParameter = null)
    //{
    //    ActionContent = content;
    //    ActionCommand = command;
    //    ActionCommandParameter = commandParameter;
    //    return this;
    //}

    /// <summary>
    /// Sets the duration override for the notification display.
    /// </summary>
    /// <param name="duration">The duration to display the notification.</param>
    /// <returns>
    /// Returns the <see cref="NotificationMessage"/> instance so that additional
    /// method calls can be chained.
    /// </returns>
    public NotificationMessage SetDuration(TimeSpan duration)
    {
        DurationOverride = duration;
        return this;
    }
}
