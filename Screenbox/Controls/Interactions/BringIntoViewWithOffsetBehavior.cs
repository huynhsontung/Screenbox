using Microsoft.Xaml.Interactivity;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls.Interactions;

internal class BringIntoViewWithOffsetBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty FromBottomProperty = DependencyProperty.Register(
        nameof(FromBottom),
        typeof(double),
        typeof(BringIntoViewWithOffsetBehavior),
        new PropertyMetadata(0.0));

    public double FromBottom
    {
        get => (double)GetValue(FromBottomProperty);
        set => SetValue(FromBottomProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is ListViewBase listView)
        {
            if (listView.ItemsPanelRoot != null)
            {
                listView.ItemsPanelRoot.BringIntoViewRequested += OnBringIntoViewRequested;
            }
            else if (!listView.IsLoaded)
            {
                listView.Loaded += OnLoaded;
            }
        }
        else
        {
            AssociatedObject.BringIntoViewRequested += OnBringIntoViewRequested;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is ListViewBase { ItemsPanelRoot: not null } listView)
        {
            listView.ItemsPanelRoot.BringIntoViewRequested -= OnBringIntoViewRequested;
        }
        else
        {
            AssociatedObject.BringIntoViewRequested -= OnBringIntoViewRequested;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (AssociatedObject is not ListViewBase listView) return;
        listView.Loaded -= OnLoaded;
        if (listView.ItemsPanelRoot != null)
        {
            listView.ItemsPanelRoot.BringIntoViewRequested += OnBringIntoViewRequested;
        }
    }

    private void OnBringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
    {
        if (FromBottom > 0 && !double.IsInfinity(FromBottom))
        {
            Rect rect = args.TargetRect;
            args.TargetRect = new Rect(rect.X, rect.Y, rect.Width, rect.Height + FromBottom);
        }
    }
}