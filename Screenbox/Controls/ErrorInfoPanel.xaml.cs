using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
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
    private const string SmileStateName = "Smile";
    private const string LaughStateName = "Laugh";
    private const string FrownStateName = "Frown";
    private const string CryStateName = "Cry";
    private const string SurpriseStateName = "Surprise";
    private const string WinkStateName = "Wink";
    private const string SkepticalStateName = "Skeptical";
    private const string NeutralStateName = "Neutral";

    private const string MessageVisibleStateName = "MessageVisible";
    private const string MessageCollapsedStateName = "MessageCollapsed";
    private const string QrCodeVisibleStateName = "QrCodeVisible";
    private const string QrCodeCollapsedStateName = "QrCodeCollapsed";
    private const string NavigateUriVisibleStateName = "NavigateUriVisible";
    private const string NavigateUriCollapsedStateName = "NavigateUriCollapsed";
    private const string DescriptionVisibleStateName = "DescriptionVisible";
    private const string DescriptionCollapsedStateName = "DescriptionCollapsed";

    /// <summary>
    /// Identifies the <see cref="Status"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(EmoticonStatus), typeof(ErrorInfoPanel), new PropertyMetadata(EmoticonStatus.Frown, OnPropertyChanged));

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
        nameof(Message), typeof(string), typeof(ErrorInfoPanel), new PropertyMetadata(null, OnPropertyChanged));

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
        nameof(QrCode), typeof(FrameworkElement), typeof(ErrorInfoPanel), new PropertyMetadata(null, OnPropertyChanged));

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
        nameof(NavigateUri), typeof(Uri), typeof(ErrorInfoPanel), new PropertyMetadata(null, OnPropertyChanged));

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
        nameof(Description), typeof(string), typeof(ErrorInfoPanel), new PropertyMetadata(null, OnPropertyChanged));

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

        UpdateStatus();
        UpdateMessageVisibility();
        UpdateQrCodeVisibility();
        UpdateNavigateUriVisibility();
        UpdateDescriptionVisibility();
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new ErrorInfoPanelAutomationPeer(this);
    }

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (ErrorInfoPanel)sender;
        if (args.Property == StatusProperty)
        {
            control.UpdateStatus();
        }
        else if (args.Property == MessageProperty)
        {
            control.UpdateMessageVisibility();
        }
        else if (args.Property == QrCodeProperty)
        {
            control.UpdateQrCodeVisibility();
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

    private void UpdateStatus()
    {
        string emoticonState = Status switch
        {
            EmoticonStatus.Smile => SmileStateName,
            EmoticonStatus.Laugh => LaughStateName,
            EmoticonStatus.Frown => FrownStateName,
            EmoticonStatus.Cry => CryStateName,
            EmoticonStatus.Surprise => SurpriseStateName,
            EmoticonStatus.Wink => WinkStateName,
            EmoticonStatus.Skeptical => SkepticalStateName,
            EmoticonStatus.Neutral => NeutralStateName,
            _ => FrownStateName
        };

        VisualStateManager.GoToState(this, emoticonState, false);
    }

    private void UpdateMessageVisibility()
    {
        VisualStateManager.GoToState(this, !string.IsNullOrEmpty(Message) ? MessageVisibleStateName : MessageCollapsedStateName, true);
    }

    private void UpdateQrCodeVisibility()
    {
        VisualStateManager.GoToState(this, QrCode != null ? QrCodeVisibleStateName : QrCodeCollapsedStateName, true);
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

public sealed class ErrorInfoPanelAutomationPeer : FrameworkElementAutomationPeer
{
    //private readonly ErrorInfoPanel _owner;
    private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();
    private readonly string _criticalErrorMoreInformation;

    public ErrorInfoPanelAutomationPeer(ErrorInfoPanel owner) : base(owner)
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
        var owner = (ErrorInfoPanel)Owner;
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
        // TODO: If there is no message, return the localized string for the emoticon status
        //else
        //{
        //}

        return name;
    }

    protected override string GetFullDescriptionCore()
    {
        string description = base.GetFullDescriptionCore();
        var owner = (ErrorInfoPanel)Owner;
        if (!string.IsNullOrEmpty(owner.Description))
        {
            description = owner.Description;
        }

        return description;
    }

    protected override string GetHelpTextCore()
    {
        string description = base.GetHelpTextCore();
        var owner = (ErrorInfoPanel)Owner;
        if (owner.NavigateUri != null)
        {
            description = $"{_criticalErrorMoreInformation}: {owner.NavigateUri}";
        }

        return description;
    }
}
