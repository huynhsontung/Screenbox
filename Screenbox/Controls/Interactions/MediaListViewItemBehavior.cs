#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Xaml.Interactivity;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls.Interactions
{
    internal class MediaListViewItemBehavior : Behavior<Control>
    {
        private readonly CommonViewModel _common;
        private SelectorItem? _selector;
        private ListViewBase? _listView;
        private ButtonBase? _playButton;
        private long _selectionModePropertyToken;

        public MediaListViewItemBehavior()
        {
            _common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject.FindAscendant<SelectorItem>() is { } selector)
            {
                Initialize(selector);
            }
            else
            {
                AssociatedObject.Loaded += AssociatedObjectOnLoaded;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            if (_selector == null) return;
            SelectorItem selector = _selector;

            selector.FocusEngaged -= SelectorFocusEngaged;
            selector.GettingFocus -= SelectorOnGettingFocus;
            selector.LostFocus -= SelectorOnLostFocus;
            selector.PointerEntered -= SelectorOnPointerEntered;
            selector.PointerExited -= SelectorOnPointerExited;
            selector.PointerCanceled -= SelectorOnPointerExited;
            selector.DoubleTapped -= SelectorOnDoubleTapped;

            if (_listView == null || _selectionModePropertyToken == default) return;
            _listView.UnregisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, _selectionModePropertyToken);
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            if (AssociatedObject.FindAscendant<SelectorItem>() is not { } selector) return;
            Initialize(selector);
        }

        private void Initialize(SelectorItem selector)
        {
            // Listen to selector interaction events
            _selector = selector;

            selector.FocusEngaged += SelectorFocusEngaged;
            selector.GettingFocus += SelectorOnGettingFocus;
            selector.LostFocus += SelectorOnLostFocus;
            selector.PointerEntered += SelectorOnPointerEntered;
            selector.PointerExited += SelectorOnPointerExited;
            selector.PointerCanceled += SelectorOnPointerExited;
            selector.DoubleTapped += SelectorOnDoubleTapped;

            // Listen to selection mode change
            if (selector.FindAscendant<ListViewBase>() is not { } listView) return;
            _listView = listView;
            _selectionModePropertyToken =
                listView.RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, OnSelectionModeChanged);
            UpdateSelectionModeVisualState((ListViewSelectionMode)listView.GetValue(ListViewBase.SelectionModeProperty));

            // Bind buttons command
            BindButtonsCommand(listView);
        }

        private void BindButtonsCommand(ListViewBase listView)
        {
            if (AssociatedObject.FindDescendant("PlayButton") is ButtonBase button)
            {
                _playButton = button;
                if (listView.Resources.TryGetValue("MediaListViewItemPlayCommand", out object value) &&
                    value is XamlUICommand command)
                {
                    // We only use XamlUICommand as a medium to pass command logic
                    button.Command = command.Command;
                }
            }

            if (AssociatedObject.FindDescendant("AlbumButton") is ButtonBase albumButton)
            {
                albumButton.Command = _common.OpenAlbumCommand;
            }

            if (AssociatedObject.FindDescendant("ArtistButton") is ButtonBase artistButton)
            {
                artistButton.Command = _common.OpenArtistCommand;
            }
        }

        private void SelectorOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _playButton?.Command?.Execute(_playButton?.CommandParameter);
        }

        private void SelectorOnLostFocus(object sender, RoutedEventArgs e)
        {
            Control? control = FocusManager.GetFocusedElement() as Control;
            if (control?.FindAscendantOrSelf<SelectorItem>() == _selector) return;
            VisualStateManager.GoToState(AssociatedObject, "Normal", false);
        }

        private void SelectorOnGettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            VisualStateManager.GoToState(AssociatedObject, "PointerOver", false);
        }

        private void SelectorOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(AssociatedObject, "Normal", false);
        }

        private void SelectorOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(AssociatedObject, "PointerOver", false);
        }

        private void SelectorFocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            _playButton?.Focus(FocusState.Programmatic);
        }

        private void OnSelectionModeChanged(DependencyObject sender, DependencyProperty dp)
        {
            ListViewSelectionMode selectionMode = (ListViewSelectionMode)sender.GetValue(dp);
            UpdateSelectionModeVisualState(selectionMode);
        }

        private void UpdateSelectionModeVisualState(ListViewSelectionMode selectionMode)
        {
            VisualStateManager.GoToState(AssociatedObject,
                selectionMode == ListViewSelectionMode.Multiple ? "MultiSelectEnabled" : "MultiSelectDisabled",
                true);
        }
    }
}
