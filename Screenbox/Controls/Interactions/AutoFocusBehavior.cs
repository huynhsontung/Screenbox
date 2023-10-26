using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Behaviors;
using System;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls.Interactions;
internal class AutoFocusBehavior : BehaviorBase<Control>
{
    public double Delay { get; set; }

    private bool _focused;
    private bool _eventTriggered;
    private readonly DispatcherQueueTimer _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();

    protected override void OnAttached()
    {
        base.OnAttached();
        _focused = false;
        _eventTriggered = false;
        if (AssociatedObject is ListViewBase { Items: { Count: 0 } items })
        {
            items.VectorChanged += ItemsOnVectorChanged;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is ListViewBase { Items: { } items })
        {
            items.VectorChanged -= ItemsOnVectorChanged;
        }
    }

    private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        if (sender.Count == 0 || _eventTriggered) return;
        sender.VectorChanged -= ItemsOnVectorChanged;
        _eventTriggered = true;
        if (!_focused)
        {
            DelayFocus(Delay);
        }
    }

    protected override void OnAssociatedObjectLoaded()
    {
        if (Delay > 0)
        {
            DelayFocus(Delay);
        }
        else
        {
            _focused = AssociatedObject.Focus(FocusState.Programmatic);
        }
    }

    private void DelayFocus(double delay)
    {
        object focused = FocusManager.GetFocusedElement();
        _timer.Debounce(() =>
        {
            if (focused == FocusManager.GetFocusedElement() && AssociatedObject != null)
            {
                _focused = AssociatedObject.Focus(FocusState.Programmatic);
            }
        }, TimeSpan.FromMilliseconds(delay));
    }
}
