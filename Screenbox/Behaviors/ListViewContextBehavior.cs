#nullable enable

using CommunityToolkit.WinUI;
using Microsoft.Xaml.Interactivity;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Screenbox.Behaviors;

/// <summary>
/// Represents a behavior that triggers actions when a context menu is requested
/// on each item of a <see cref="ListViewBase"/> control.
/// </summary>
/// <remarks>
/// This behavior listens for context menu requests and right-tap events on the
/// associated <see cref="ListViewBase"/> control and executes the specified actions.
/// </remarks>
internal sealed partial class ListViewContextBehavior : Behavior<ListViewBase>
{
    /// <summary>
    /// Occurs when the user has completed a context menu request on an item in the
    /// associated <see cref="ListViewBase"/> control.
    /// </summary>
    public event TypedEventHandler<ListViewContextBehavior, ListViewContextRequestedEventArgs>? ContextRequested;

    /// <summary>
    /// Identifies the <see cref="Flyout"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FlyoutProperty = DependencyProperty.Register(
        nameof(Flyout),
        typeof(FlyoutBase),
        typeof(ListViewContextBehavior),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the flyout associated with each item in this <see cref="ListViewBase"/>
    /// control.
    /// </summary>
    /// <value>
    /// The flyout associated with each item, if any; otherwise, <b><see langword="null"/></b>.
    /// The default is <see langword="null"/>.
    /// </value>
    public FlyoutBase? Flyout
    {
        get => (FlyoutBase?)GetValue(FlyoutProperty);
        set => SetValue(FlyoutProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ContextItem"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContextItemProperty = DependencyProperty.Register(
        nameof(ContextItem),
        typeof(object),
        typeof(ListViewContextBehavior),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content of the item that was right-clicked or context requested.
    /// </summary>
    /// <value>The content of the item that was right-clicked or context requested.</value>
    public object? ContextItem
    {
        get => GetValue(ContextItemProperty);
        set => SetValue(ContextItemProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.ContextRequested += OnContextRequested;
        AssociatedObject.RightTapped += OnRightTapped;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.ContextRequested -= OnContextRequested;
        AssociatedObject.RightTapped -= OnRightTapped;
    }

    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (args.OriginalSource is not SelectorItem item)
            return;

        var context = item.Content;
        ContextItem = context;

        var itemArgs = new ListViewContextRequestedEventArgs(item);
        ContextRequested?.Invoke(this, itemArgs);

        if (itemArgs.Handled)
            return;

        Flyout?.ShowAt(item);
        args.Handled = true;
    }

    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not FrameworkElement element ||
            element.FindAscendantOrSelf<SelectorItem>() is not { } item)
            return;

        var context = item.Content;
        ContextItem = context;

        var itemArgs = new ListViewContextRequestedEventArgs(item);
        ContextRequested?.Invoke(this, itemArgs);

        if (itemArgs.Handled)
            return;

        if (Flyout is MenuFlyout menuFlyout)
        {
            menuFlyout.ShowAt(element, e.GetPosition(element));
        }
        else
        {
            Flyout?.ShowAt(element);
        }

        e.Handled = true;
    }
}
