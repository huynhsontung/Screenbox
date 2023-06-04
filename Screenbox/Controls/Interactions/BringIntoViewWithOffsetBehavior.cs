using Microsoft.Xaml.Interactivity;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls.Interactions;

internal class BringIntoViewWithOffsetBehavior : Behavior<ListViewBase>
{
    public static readonly DependencyProperty FromBottomProperty = DependencyProperty.Register(
        nameof(FromBottom), typeof(double), typeof(BringIntoViewWithOffsetBehavior), new PropertyMetadata(0.0));

    public double FromBottom
    {
        get => (double)GetValue(FromBottomProperty);
        set => SetValue(FromBottomProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject.ItemsPanelRoot != null)
        {
            AssociatedObject.ItemsPanelRoot.BringIntoViewRequested += OnBringIntoViewRequested;
        }
        else if (!AssociatedObject.IsLoaded)
        {
            AssociatedObject.Loaded += OnLoaded;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject.ItemsPanelRoot != null)
        {
            AssociatedObject.ItemsPanelRoot.BringIntoViewRequested -= OnBringIntoViewRequested;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Loaded -= OnLoaded;
        if (AssociatedObject.ItemsPanelRoot != null)
        {
            AssociatedObject.ItemsPanelRoot.BringIntoViewRequested += OnBringIntoViewRequested;
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