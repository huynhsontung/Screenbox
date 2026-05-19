using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;

namespace Screenbox.Controls;

/// <summary>
/// Exposes <see cref="ContentUnavailableView"/> types to <a href="https://learn.microsoft.com/en-us/windows/win32/winauto/entry-uiauto-win32">Microsoft UI Automation</a>.
/// </summary>
public sealed class ContentUnavailableViewAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentUnavailableViewAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="ContentUnavailableView"/> to create the peer for.</param>
    public ContentUnavailableViewAutomationPeer(FrameworkElement owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.StatusBar;
    }

    protected override string GetClassNameCore()
    {
        return nameof(ContentUnavailableView);
    }

    internal void RaiseIsOpen(string displayString)
    {
        RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            displayString,
            "ContentUnavailableViewIsOpenActivityId");
    }
}
