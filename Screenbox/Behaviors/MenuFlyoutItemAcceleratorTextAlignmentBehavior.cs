using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Screenbox.Behaviors;

/// <summary>
/// Provides a behavior that automatically adjusts the text alignment of the
/// keyboard accelerator text in a <see cref="MenuFlyoutItem"/> to be right-aligned.
/// </summary>
/// <remarks>
/// Workaround for <a href="https://github.com/microsoft/microsoft-ui-xaml/issues/8755">microsoft/microsoft-ui-xaml#8755</a>.
/// <para>When a menu flyout is opened for the second time, the keyboard accelerator text
/// in a <b>MenuFlyoutItem</b> may not be aligned to the right as expected. This behavior
/// backports the fix from <a href="https://github.com/microsoft/microsoft-ui-xaml/releases/tag/winui3%2Frelease%2F1.5.0">WinUI 3 1.5.0</a>.</para>
/// </remarks>
public sealed class MenuFlyoutItemAcceleratorTextAlignmentBehavior : Behavior<MenuFlyoutItem>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded += AssociatedObject_OnLoaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Loaded -= AssociatedObject_OnLoaded;
    }

    [DynamicWindowsRuntimeCast(typeof(MenuFlyoutItem))]
    [DynamicWindowsRuntimeCast(typeof(Grid))]
    [DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private void AssociatedObject_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuFlyoutItem)
            return;

        if (VisualTreeHelper.GetChild(menuFlyoutItem, 0) is Grid layoutRootGrid)
        {
            if (layoutRootGrid.FindName("KeyboardAcceleratorTextBlock") is TextBlock textBlock)
            {
                textBlock.TextAlignment = TextAlignment.Right;
            }
        }
    }
}
