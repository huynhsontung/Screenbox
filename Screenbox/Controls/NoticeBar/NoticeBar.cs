using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

/// <summary>
/// Represents a control that displays placeholder content when no data is present.
/// </summary>
/// <remarks>
/// Once the control is open, any changes made to the various properties, like updating
/// the message, will not raise an automation notification. To ensure screen reader users
/// are notified of new content, close and re-open the control.
/// </remarks>
/// <example>
/// <code language="xml"><![CDATA[
/// <local:NoticeBar Title="This folder is empty"
///                  Message="There are no images to display. Add files to it to get started."
///                  IsOpen="True">
///     <local:NoticeBar.Content>
///         <FontIcon Glyph="&#xE8B9;" />
///     </local:NoticeBar.Content>
///     <local:NoticeBar.ActionContent>
///         <Button Content="Add folder" Click="AddImagesButton_Click" />
///     </local:NoticeBar.ActionContent>
/// </local:NoticeBar>
/// ]]></code>
/// </example>
public sealed partial class NoticeBar : ContentControl
{
    private const string NoticeBarCollapsedStateName = "NoticeBarCollapsed";
    private const string NoticeBarVisibleStateName = "NoticeBarVisible";
    private const string ContentCollapsedStateName = "ContentCollapsed";
    private const string ContentVisibleStateName = "ContentVisible";
    private const string TitleTextBlockCollapsedStateName = "TitleTextBlockCollapsed";
    private const string TitleTextBlockVisibleStateName = "TitleTextBlockVisible";
    private const string MessageTextBlockCollapsedStateName = "MessageTextBlockCollapsed";
    private const string MessageTextBlockVisibleStateName = "MessageTextBlockVisible";
    private const string ActionContentCollapsedStateName = "ActionContentCollapsed";
    private const string ActionContentVisibleStateName = "ActionContentVisible";

    /// <summary>
    /// Initializes a new instance of the <see cref="NoticeBar"/> class.
    /// </summary>
    public NoticeBar()
    {
        this.DefaultStyleKey = typeof(NoticeBar);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new NoticeBarAutomationPeer(this);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UpdateVisibility();
        UpdateContent();
        UpdateTitle();
        UpdateMessage();
        UpdateActionContent();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        UpdateContent();
    }

    protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
    {
        UpdateContent();
    }

    private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
    {
        var property = args.Property;

        if (property == IsOpenProperty)
        {
            UpdateVisibility();
        }
        else if (property == ContentProperty || property == ContentTemplateProperty)
        {
            UpdateContent();
        }
        else if (property == TitleProperty)
        {
            UpdateTitle();
        }
        else if (property == MessageProperty)
        {
            UpdateMessage();
        }
        else if (property == ActionContentProperty)
        {
            UpdateActionContent();
        }
    }

    private void UpdateVisibility()
    {
        if (IsOpen)
        {
            if (FrameworkElementAutomationPeer.FromElement(this) is NoticeBarAutomationPeer peer)
            {
                string notificationString;
                if (!string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Message))
                {
                    notificationString = $"{Title}; {Message}";
                }
                else if (!string.IsNullOrWhiteSpace(Title))
                {
                    notificationString = Title;
                }
                else
                {
                    notificationString = Message ?? string.Empty;
                }

                peer.RaiseIsOpen(notificationString);
            }

            VisualStateManager.GoToState(this, NoticeBarVisibleStateName, false);
            AutomationProperties.SetAccessibilityView(this, AccessibilityView.Control);
        }
        else
        {
            VisualStateManager.GoToState(this, NoticeBarCollapsedStateName, false);
            AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
        }
    }

    private void UpdateContent()
    {
        string stateName = Content is not null
            ? ContentVisibleStateName
            : ContentCollapsedStateName;

        VisualStateManager.GoToState(this, stateName, true);
    }

    private void UpdateTitle()
    {
        string stateName = string.IsNullOrEmpty(Title)
            ? TitleTextBlockCollapsedStateName
            : TitleTextBlockVisibleStateName;

        VisualStateManager.GoToState(this, stateName, true);
    }

    private void UpdateMessage()
    {
        string stateName = string.IsNullOrEmpty(Message)
            ? MessageTextBlockCollapsedStateName
            : MessageTextBlockVisibleStateName;

        VisualStateManager.GoToState(this, stateName, true);
    }

    private void UpdateActionContent()
    {
        string stateName = ActionContent is not null
            ? ActionContentVisibleStateName
            : ActionContentCollapsedStateName;

        VisualStateManager.GoToState(this, stateName, true);
    }
}
