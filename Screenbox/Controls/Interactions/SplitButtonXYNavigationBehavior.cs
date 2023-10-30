using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Behaviors;
using Windows.UI.Xaml.Controls;
using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;

namespace Screenbox.Controls.Interactions;
internal class SplitButtonXYNavigationBehavior : BehaviorBase<SplitButton>
{
    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();
        if (AssociatedObject.FindDescendant<Button>(b => b.Name == "SecondaryButton") is { } secondaryButton)
        {
            secondaryButton.IsTabStop = true;
            secondaryButton.XYFocusRight = AssociatedObject;
            AssociatedObject.XYFocusRight = secondaryButton;
        }
    }
}
