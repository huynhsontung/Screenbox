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

    private DateTimeOffset _deferredStart;
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
            _deferredStart = DateTimeOffset.Now;
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
        TimeSpan delta = DateTimeOffset.Now - _deferredStart;
        if (!_focused && delta < TimeSpan.FromSeconds(1))
        {
            _timer.Debounce(() =>
            {
                if (AssociatedObject != null)
                {
                    _focused = AssociatedObject.Focus(FocusState.Programmatic);
                }
            }, TimeSpan.FromMilliseconds(Delay));
        }
    }

    protected override void OnAssociatedObjectLoaded()
    {
        if (Delay > 0)
        {
            object focused = FocusManager.GetFocusedElement();
            _timer.Debounce(() =>
            {
                if (focused == FocusManager.GetFocusedElement() && AssociatedObject != null)
                {
                    _focused = AssociatedObject.Focus(FocusState.Programmatic);
                }
            }, TimeSpan.FromMilliseconds(Delay));
        }
        else
        {
            _focused = AssociatedObject.Focus(FocusState.Programmatic);
        }
    }
}
