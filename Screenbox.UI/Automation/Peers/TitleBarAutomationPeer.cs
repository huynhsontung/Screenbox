// Copyright (c) Tung Huynh and Contributors. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Screenbox.UI.Controls;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Automation.Peers;

namespace Screenbox.UI.Automation.Peers;

/// <summary>
/// Exposes <see cref="TitleBar"/> types to <a href="https://learn.microsoft.com/en-us/windows/win32/winauto/entry-uiauto-win32">Microsoft UI Automation</a>.
/// </summary>
public sealed partial class TitleBarAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TitleBarAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="TitleBar"/> to create the peer for.</param>
    public TitleBarAutomationPeer(TitleBar owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.TitleBar;
    }

    protected override string GetClassNameCore()
    {
        return nameof(TitleBar);
    }

    protected override string GetNameCore()
    {
        string name = base.GetNameCore();

        if (string.IsNullOrWhiteSpace(name))
        {
            var owner = (TitleBar)Owner;
            name = string.IsNullOrWhiteSpace(owner.Title)
                ? AppInfo.Current.DisplayInfo.DisplayName
                : owner.Title;
        }

        return name;
    }
}
