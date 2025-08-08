using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Screenbox.Controls;

/// <summary>
/// Defines constants that specify a emoticon to use as the severity level of a <see cref="ErrorInfo"/>.
/// </summary>
/// <seealso href="https://windowsteamblog.com/windows_phone/b/windowsphone/archive/2011/11/29/tip-tuesday-express-yourself-with-emoji.aspx"/>
public enum Emoticon
{
    Smiley,
    Laugh,
    Wink,
    Surprise,
    Angry,
    Confused,
    Embarrassed,
    Sad,
    Cry,
    Disappointed,
    Annoyed,
    Skeptical,
}

/// <summary>
/// Represents a control that displays error information, including an emoticon, message,
/// description, optional QR code, and an optional hyperlink.
/// </summary>
/// <remarks>
/// Use a ErrorInfo control to show information about a error to the user, typically occupying the entire view.
/// <para>Use the <see cref="Emoticon"/> property to indicate the severity of the error.</para>
/// <para>Use the <see cref="Message"/> property to provide a concise message of the error,
/// and the <see cref="Description"/> property co show the corresponding error code.</para>
/// <para>Use the <see cref="QrCodeContent"/> property (through a <see cref="FrameworkElement"/> such as an image or shape),
/// and the <see cref="NavigateUri"/> property to direct the user to a location that provides support or documentation.</para>
/// </remarks>
[ContentProperty(Name = "QrCodeContent")]
public sealed partial class ErrorInfo : UserControl
{
    private const string SmileyStateName = "Smiley";
    private const string LaughStateName = "Laugh";
    private const string WinkStateName = "Wink";
    private const string SurpriseStateName = "Surprise";
    private const string AngryStateName = "Angry";
    private const string ConfusedStateName = "Confused";
    private const string EmbarrassedStateName = "Embarrassed";
    private const string SadStateName = "Sad";
    private const string CryStateName = "Cry";
    private const string DisappointedStateName = "Disappointed";
    private const string AnnoyedStateName = "Annoyed";
    private const string SkepticalStateName = "Skeptical";

    private const string MessageVisibleStateName = "MessageVisible";
    private const string MessageCollapsedStateName = "MessageCollapsed";
    private const string QrCodeContentVisibleStateName = "QrCodeContentVisible";
    private const string QrCodeContentCollapsedStateName = "QrCodeContentCollapsed";
    private const string NavigateUriVisibleStateName = "NavigateUriVisible";
    private const string NavigateUriCollapsedStateName = "NavigateUriCollapsed";
    private const string DescriptionVisibleStateName = "DescriptionVisible";
    private const string DescriptionCollapsedStateName = "DescriptionCollapsed";

    /// <summary>
    /// Identifies the <see cref="Emoticon"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EmoticonProperty = DependencyProperty.Register(
        nameof(Emoticon), typeof(Emoticon), typeof(ErrorInfo), new PropertyMetadata(Emoticon.Sad, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the emoticon that indicates the severity level of the <see cref="ErrorInfo"/>.
    /// </summary>
    public Emoticon Emoticon
    {
        get { return (Emoticon)GetValue(EmoticonProperty); }
        set { SetValue(EmoticonProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Message"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(ErrorInfo), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the message of the <see cref="ErrorInfo"/>.
    /// </summary>
    public string Message
    {
        get { return (string)GetValue(MessageProperty); }
        set { SetValue(MessageProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="QrCodeContent"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty QrCodeContentProperty = DependencyProperty.Register(
        nameof(QrCodeContent), typeof(FrameworkElement), typeof(ErrorInfo), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets an arbitrary <see cref="FrameworkElement"/> that can be used to display a QR code.
    /// </summary>
    public FrameworkElement QrCodeContent
    {
        get { return (FrameworkElement)GetValue(QrCodeContentProperty); }
        set { SetValue(QrCodeContentProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="NavigateUri"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty NavigateUriProperty = DependencyProperty.Register(
        nameof(NavigateUri), typeof(Uri), typeof(ErrorInfo), new PropertyMetadata(null, OnPropertyChanged));

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
        nameof(Description), typeof(string), typeof(ErrorInfo), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the description of the <see cref="ErrorInfo"/>.
    /// </summary>
    public string Description
    {
        get { return (string)GetValue(DescriptionProperty); }
        set { SetValue(DescriptionProperty, value); }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorInfo"/> class.
    /// </summary>
    public ErrorInfo()
    {
        InitializeComponent();

        UpdateEmoticon();
        UpdateMessageVisibility();
        UpdateQrCodeContentVisibility();
        UpdateNavigateUriVisibility();
        UpdateDescriptionVisibility();
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new ErrorInfoAutomationPeer(this);
    }

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (ErrorInfo)sender;
        if (args.Property == EmoticonProperty)
        {
            control.UpdateEmoticon();
        }
        else if (args.Property == MessageProperty)
        {
            control.UpdateMessageVisibility();
        }
        else if (args.Property == QrCodeContentProperty)
        {
            control.UpdateQrCodeContentVisibility();
        }
        else if (args.Property == NavigateUriProperty)
        {
            control.UpdateNavigateUriVisibility();
        }
        else if (args.Property == DescriptionProperty)
        {
            control.UpdateDescriptionVisibility();
        }
    }

    private void UpdateEmoticon()
    {
        string emoticonState = Emoticon switch
        {
            Emoticon.Smiley => SmileyStateName,
            Emoticon.Laugh => LaughStateName,
            Emoticon.Wink => WinkStateName,
            Emoticon.Surprise => SurpriseStateName,
            Emoticon.Angry => AngryStateName,
            Emoticon.Confused => ConfusedStateName,
            Emoticon.Embarrassed => EmbarrassedStateName,
            Emoticon.Sad => SadStateName,
            Emoticon.Cry => CryStateName,
            Emoticon.Disappointed => DisappointedStateName,
            Emoticon.Annoyed => AnnoyedStateName,
            Emoticon.Skeptical => SkepticalStateName,
            _ => SadStateName
        };

        VisualStateManager.GoToState(this, emoticonState, false);
    }

    private void UpdateMessageVisibility()
    {
        VisualStateManager.GoToState(this, !string.IsNullOrEmpty(Message) ? MessageVisibleStateName : MessageCollapsedStateName, true);
    }

    private void UpdateQrCodeContentVisibility()
    {
        VisualStateManager.GoToState(this, QrCodeContent != null ? QrCodeContentVisibleStateName : QrCodeContentCollapsedStateName, true);
    }

    private void UpdateNavigateUriVisibility()
    {
        VisualStateManager.GoToState(this, NavigateUri != null ? NavigateUriVisibleStateName : NavigateUriCollapsedStateName, true);
    }

    private void UpdateDescriptionVisibility()
    {
        VisualStateManager.GoToState(this, !string.IsNullOrEmpty(Description) ? DescriptionVisibleStateName : DescriptionCollapsedStateName, true);
    }
}

public sealed class ErrorInfoAutomationPeer : FrameworkElementAutomationPeer
{
    //private readonly ErrorInfo _owner;
    private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();
    private readonly string _criticalErrorMoreInformation;

    public ErrorInfoAutomationPeer(ErrorInfo owner) : base(owner)
    {
        _criticalErrorMoreInformation = _resourceLoader.GetString("CriticalErrorMoreInformation");
    }

    //protected override AutomationControlType GetAutomationControlTypeCore()
    //{
    //    return AutomationControlType.Group;
    //}

    //protected override string GetLocalizedControlTypeCore()
    //{
    //    return nameof(Panel);
    //}

    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        var owner = (ErrorInfo)Owner;
        if (!string.IsNullOrEmpty(owner.Message))
        {
            if (owner.NavigateUri != null)
            {
                name = $"{owner.Message}; {_criticalErrorMoreInformation}:";
            }
            else
            {
                name = owner.Message;
            }
        }
        // TODO: If there is no message, return the localized string for the emoticon
        //else
        //{
        //}

        return name;
    }

    protected override string GetFullDescriptionCore()
    {
        string description = base.GetFullDescriptionCore();
        var owner = (ErrorInfo)Owner;
        if (!string.IsNullOrEmpty(owner.Description))
        {
            description = owner.Description;
        }

        return description;
    }

    protected override string GetHelpTextCore()
    {
        string description = base.GetHelpTextCore();
        var owner = (ErrorInfo)Owner;
        if (owner.NavigateUri != null)
        {
            description = $"{_criticalErrorMoreInformation}: {owner.NavigateUri}";
        }

        return description;
    }
}
