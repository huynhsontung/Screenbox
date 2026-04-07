using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;

namespace Screenbox.Controls;

/// <summary>
/// Exposes <see cref="EmptyStateBar"/> types to <a href="https://learn.microsoft.com/en-us/windows/win32/winauto/entry-uiauto-win32">Microsoft UI Automation</a>.
/// </summary>
public sealed class EmptyStateBarAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyStateBarAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="EmptyStateBar"/> to create the peer for.</param>
    public EmptyStateBarAutomationPeer(FrameworkElement owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.StatusBar;
    }

    protected override string GetClassNameCore()
    {
        return nameof(EmptyStateBar);
    }

    internal void RaiseIsOpen(string displayString)
    {
        RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            displayString,
            "EmptyStateBarIsOpenActivityId");
    }
}
