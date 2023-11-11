#nullable enable

using CommunityToolkit.WinUI;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Controls.Interactions;

internal class FocusOnItemClickBehavior : Behavior<ListViewBase>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ItemClick += OnItemClick;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ItemClick -= OnItemClick;
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        SelectorItem? item = (SelectorItem?)AssociatedObject.ContainerFromItem(e.ClickedItem);
        item?.FindDescendant<ButtonBase>()?.Focus(FocusState.Programmatic);
    }
}