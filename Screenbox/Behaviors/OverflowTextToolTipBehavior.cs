using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Behaviors;
public class OverflowTextToolTipBehavior : Behavior<TextBlock>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.IsTextTrimmedChanged += OnIsTextTrimmedChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.IsTextTrimmedChanged -= OnIsTextTrimmedChanged;
    }

    private static void OnIsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(sender.Text)) return;
        ToolTipService.SetToolTip(sender, sender.IsTextTrimmed ? sender.Text : null);
    }
}
