#nullable enable

using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using System;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Behaviors;
internal class AutoFocusBehavior : BehaviorBase<Control>
{
    private bool _focused;
    private bool _eventTriggered;
    private FrameworkElement? _deferredElement;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _timer;

    public AutoFocusBehavior()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _timer = _dispatcherQueue.CreateTimer();
    }

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
        ResetDeferredElement();
        if (AssociatedObject is ListViewBase { Items: { } items })
        {
            items.VectorChanged -= ItemsOnVectorChanged;
        }
    }

    private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        if (sender.Count == 0 || _eventTriggered) return;
        sender.VectorChanged -= ItemsOnVectorChanged;
        // Event may fire multiple times synchronously.
        // Handler is called multiple times even after unregister
        _eventTriggered = true;
        if (!_focused)
        {
            _timer.Debounce(() =>
            {
                if (AssociatedObject != null)
                {
                    _focused = AssociatedObject.Focus(FocusState.Programmatic);
                }
            }, TimeSpan.FromMilliseconds(80));
        }
    }

    protected override void OnAssociatedObjectLoaded()
    {
        // Check if Space key is still down
        bool spaceDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Space).HasFlag(CoreVirtualKeyStates.Down);
        bool enterDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Enter).HasFlag(CoreVirtualKeyStates.Down);
        bool gamepadADown = Window.Current.CoreWindow.GetKeyState(VirtualKey.GamepadA).HasFlag(CoreVirtualKeyStates.Down);
        bool triggered = spaceDown || enterDown || gamepadADown;

        // If yes than wait until key up then focus
        if (triggered && FocusManager.GetFocusedElement() is FrameworkElement element)
        {
            ResetDeferredElement();
            element.PreviewKeyUp -= ElementOnPreviewKeyUp;
            element.PreviewKeyUp += ElementOnPreviewKeyUp;
            _deferredElement = element;
        }
        else
        {
            _focused = AssociatedObject.Focus(FocusState.Programmatic);
        }
    }

    private void ElementOnPreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        FrameworkElement element = (FrameworkElement)sender;
        element.PreviewKeyUp -= ElementOnPreviewKeyUp;
        if (e.Key is VirtualKey.Space or VirtualKey.Enter or VirtualKey.GamepadA && AssociatedObject != null)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                _focused = AssociatedObject.Focus(FocusState.Programmatic);
            });
        }
    }

    private void ResetDeferredElement()
    {
        if (_deferredElement == null) return;
        _deferredElement.PreviewKeyUp -= ElementOnPreviewKeyUp;
        _deferredElement = null;
    }
}
