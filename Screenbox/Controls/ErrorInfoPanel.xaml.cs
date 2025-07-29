using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Screenbox.Controls;

/// <summary>
/// Defines constants that indicate the emoticon displayed on the <see cref="ErrorInfoPanel"/>.
/// </summary>
public enum EmoticonStatus
{
    Smile,
    Laugh,
    Frown,
    Cry,
    Surprise,
    Wink,
    Skeptical,
    Neutral
}

/// <summary>
/// A user control that displays a critical error message with an emoticon, message, error code, and help link.
/// </summary>
[ContentProperty(Name = "QrCode")]
public sealed partial class ErrorInfoPanel : UserControl
{
    /// <summary>
    /// Identifies the <see cref="Status"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(EmoticonStatus), typeof(ErrorInfoPanel), new PropertyMetadata(EmoticonStatus.Frown, OnStatusPropertyChanged));

    /// <summary>
    /// Gets or sets the emoticon that indicates the severity level of the <see cref="ErrorInfoPanel"/>.
    /// </summary>
    public EmoticonStatus Status
    {
        get { return (EmoticonStatus)GetValue(StatusProperty); }
        set { SetValue(StatusProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Message"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(ErrorInfoPanel), new PropertyMetadata(null, OnMessagePropertyChanged));

    /// <summary>
    /// Gets or sets the message of the <see cref="ErrorInfoPanel"/>.
    /// </summary>
    public string Message
    {
        get { return (string)GetValue(MessageProperty); }
        set { SetValue(MessageProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="QrCode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty QrCodeProperty = DependencyProperty.Register(
        nameof(QrCode), typeof(FrameworkElement), typeof(ErrorInfoPanel), new PropertyMetadata(null, OnQrCodePropertyChanged));

    /// <summary>
    /// Gets or sets an arbitrary <see cref="FrameworkElement"/> that can be used to display a QR code.
    /// </summary>
    public FrameworkElement QrCode
    {
        get { return (FrameworkElement)GetValue(QrCodeProperty); }
        set { SetValue(QrCodeProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="NavigateUri"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty NavigateUriProperty = DependencyProperty.Register(
        nameof(NavigateUri), typeof(Uri), typeof(ErrorInfoPanel), new PropertyMetadata(null, OnNavigateUriPropertyChanged));

    /// <summary>
    /// Gets or sets the Uniform Resource Identifier (URI) to navigate to when the <see cref="Windows.UI.Xaml.Documents.Hyperlink"/> is clicked.
    /// </summary>
    public Uri NavigateUri
    {
        get { return (Uri)GetValue(NavigateUriProperty); }
        set { SetValue(NavigateUriProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Description"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(ErrorInfoPanel), new PropertyMetadata(null, OnDescriptionPropertyChanged));

    /// <summary>
    /// Gets or sets the description of the <see cref="ErrorInfoPanel"/>.
    /// </summary>
    public string Description
    {
        get { return (string)GetValue(DescriptionProperty); }
        set { SetValue(DescriptionProperty, value); }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorInfoPanel"/> class.
    /// </summary>
    public ErrorInfoPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        UpdateStatus();
        UpdateVisibility();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateAutomationName();
    }

    private static void OnStatusPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var errorControl = (ErrorInfoPanel)sender;
        errorControl.UpdateStatus();
    }

    private static void OnMessagePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var errorControl = (ErrorInfoPanel)sender;
        errorControl.UpdateMessageVisibility();
    }

    private static void OnQrCodePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var errorControl = (ErrorInfoPanel)sender;
        errorControl.UpdateQrCodeVisibility();
    }

    private static void OnNavigateUriPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var errorControl = (ErrorInfoPanel)sender;
        errorControl.UpdateNavigateUriVisibility();
    }

    private static void OnDescriptionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var errorControl = (ErrorInfoPanel)sender;
        errorControl.UpdateDescriptionVisibility();
    }

    private void UpdateAutomationName()
    {
        string automationName;

        // The AutomationName for the control is in the format: Description, Message, HelpLink.
        // If none exist, it defaults to an empty string.
        if (!string.IsNullOrEmpty(Message) && NavigateUri != null && !string.IsNullOrEmpty(Description))
        {
            automationName = $"{Description}, {Message}, {HelpLinkTextBlock.Text}";
        }
        else if (!string.IsNullOrEmpty(Message) && NavigateUri != null)
        {
            automationName = $"{Message}, {HelpLinkTextBlock.Text}";
        }
        else if (!string.IsNullOrEmpty(Description) && NavigateUri != null)
        {
            automationName = $"{Description}, {HelpLinkTextBlock.Text}";
        }
        else if (!string.IsNullOrEmpty(Message))
        {
            automationName = Message;
        }
        else if (NavigateUri != null)
        {
            automationName = HelpLinkTextBlock.Text;
        }
        else if (!string.IsNullOrEmpty(Description))
        {
            automationName = Description;
        }
        else
        {
            automationName = "";
        }

        AutomationProperties.SetName(this, automationName);
    }

    private void UpdateStatus()
    {
        string emoticonState = "Frown";

        switch (Status)
        {
            case EmoticonStatus.Smile:
                emoticonState = "Smile";
                break;
            case EmoticonStatus.Laugh:
                emoticonState = "Laugh";
                break;
            case EmoticonStatus.Frown:
                emoticonState = "Frown";
                break;
            case EmoticonStatus.Cry:
                emoticonState = "Cry";
                break;
            case EmoticonStatus.Surprise:
                emoticonState = "Surprise";
                break;
            case EmoticonStatus.Wink:
                emoticonState = "Wink";
                break;
            case EmoticonStatus.Skeptical:
                emoticonState = "Skeptical";
                break;
            case EmoticonStatus.Neutral:
                emoticonState = "Neutral";
                break;
        }

        VisualStateManager.GoToState(this, emoticonState, false);
    }

    ///// <summary>
    ///// Converts an <see cref="EmoticonStatus"/> value to its string representation.
    ///// </summary>
    ///// <param name="emoticon">The emoticon value.</param>
    ///// <returns>The string representation of the emoticon.</returns>
    //private string GetEmoticonStatusText(EmoticonStatus emoticon)
    //{
    //    return emoticon switch
    //    {
    //        EmoticonStatus.Smile => ":)",
    //        EmoticonStatus.Laugh => ":D",
    //        EmoticonStatus.Frown => ":(",
    //        EmoticonStatus.Cry => ":'(",
    //        EmoticonStatus.Surprise => ":O",
    //        EmoticonStatus.Skeptical => ":/",
    //        EmoticonStatus.Wink => ";)",
    //        EmoticonStatus.Neutral => ":|",
    //        _ => string.Empty
    //    };
    //}

    private void UpdateMessageVisibility()
    {
        VisualStateManager.GoToState(this, !string.IsNullOrEmpty(Message) ? "MessageVisible" : "MessageCollapsed", true);
    }

    private void UpdateQrCodeVisibility()
    {
        VisualStateManager.GoToState(this, QrCode != null ? "QrCodeVisible" : "QrCodeCollapsed", true);
    }

    private void UpdateNavigateUriVisibility()
    {
        VisualStateManager.GoToState(this, NavigateUri != null ? "NavigateUriVisible" : "NavigateUriCollapsed", true);
    }

    private void UpdateDescriptionVisibility()
    {
        VisualStateManager.GoToState(this, !string.IsNullOrEmpty(Description) ? "DescriptionVisible" : "DescriptionCollapsed", true);
    }

    private void UpdateVisibility()
    {
        UpdateMessageVisibility();
        UpdateQrCodeVisibility();
        UpdateNavigateUriVisibility();
        UpdateDescriptionVisibility();
    }
}
