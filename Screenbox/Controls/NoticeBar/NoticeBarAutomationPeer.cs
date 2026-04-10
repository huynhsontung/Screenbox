using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;

namespace Screenbox.Controls;

/// <summary>
/// Exposes <see cref="NoticeBar"/> types to <a href="https://learn.microsoft.com/en-us/windows/win32/winauto/entry-uiauto-win32">Microsoft UI Automation</a>.
/// </summary>
public sealed class NoticeBarAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoticeBarAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="NoticeBar"/> to create the peer for.</param>
    public NoticeBarAutomationPeer(FrameworkElement owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.StatusBar;
    }

    protected override string GetClassNameCore()
    {
        return nameof(NoticeBar);
    }

    internal void RaiseIsOpen(string displayString)
    {
        RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            displayString,
            "NoticeBarIsOpenActivityId");
    }
}
