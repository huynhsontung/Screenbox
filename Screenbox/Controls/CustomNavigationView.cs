#nullable enable

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Animations;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;

namespace Screenbox.Controls
{
    public sealed class CustomNavigationView : NavigationView
    {
        public static readonly DependencyProperty OverlayContentProperty = DependencyProperty.Register(
            nameof(OverlayContent),
            typeof(UIElement),
            typeof(CustomNavigationView),
            new PropertyMetadata(null, OnOverlayContentChanged));

        public static readonly DependencyProperty OverlayZIndexProperty = DependencyProperty.Register(
            nameof(OverlayZIndex),
            typeof(int),
            typeof(CustomNavigationView),
            new PropertyMetadata(0, OnOverlayZIndexChanged));

        public static readonly DependencyProperty ContentVisibilityProperty = DependencyProperty.Register(
            nameof(ContentVisibility),
            typeof(Visibility),
            typeof(CustomNavigationView),
            new PropertyMetadata(Visibility.Visible, OnContentVisibilityChanged));

        /// <summary>
        /// Visibility of everything except the overlay element.
        /// </summary>
        public Visibility ContentVisibility
        {
            get => (Visibility)GetValue(ContentVisibilityProperty);
            set => SetValue(ContentVisibilityProperty, value);
        }

        /// <summary>
        /// Canvas.ZIndex of the overlay element. Set a value above 1 to render on top of the nav pane. Default is 0.
        /// </summary>
        public int OverlayZIndex
        {
            get => (int)GetValue(OverlayZIndexProperty);
            set => SetValue(OverlayZIndexProperty, value);
        }

        /// <summary>
        /// Content of the overlay element that has the same level with the nav pane.
        /// </summary>
        public UIElement? OverlayContent
        {
            get => (UIElement?)GetValue(OverlayContentProperty);
            set => SetValue(OverlayContentProperty, value);
        }

        private readonly Border _overlayRoot;
        private SplitView? _splitView;
        private Grid? _paneToggleButtonGrid;
        private Grid? _contentGrid;
        private Grid? _paneContentGrid;

        public CustomNavigationView()
        {
            _overlayRoot = new Border();
            Grid.SetColumnSpan(_overlayRoot, 2);
            _overlayRoot.Tapped += OverlayRootOnTapped;
            Loaded += OnLoaded;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _splitView = (SplitView?)GetTemplateChild("RootSplitView");
            _paneToggleButtonGrid = (Grid?)GetTemplateChild("PaneToggleButtonGrid");
            _contentGrid = (Grid?)GetTemplateChild("ContentGrid");
            _paneContentGrid = (Grid?)GetTemplateChild("PaneContentGrid");

            SetContentVisibility(ContentVisibility);

            // Set implicit animations to play when ContentVisibility changes
            _paneToggleButtonGrid?.SetValue(Implicit.ShowAnimationsProperty, GetShowAnimations());
            _paneToggleButtonGrid?.SetValue(Implicit.HideAnimationsProperty, GetHideAnimations());
            _contentGrid?.SetValue(Implicit.ShowAnimationsProperty, GetShowAnimations());
            _contentGrid?.SetValue(Implicit.HideAnimationsProperty, GetHideAnimations());

            // Don't set implicit animations on _paneContentGrid because of conflict with base NavView
            // _paneContentGrid?.SetValue(Implicit.ShowAnimationsProperty, GetShowAnimations());
            // _paneContentGrid?.SetValue(Implicit.HideAnimationsProperty, GetHideAnimations());
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_splitView?.FindDescendant<Grid>() is { } splitViewRoot)
            {
                splitViewRoot.Children.Add(_overlayRoot);
            }
        }

        private void OverlayRootOnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (DisplayMode != NavigationViewDisplayMode.Expanded && IsPaneOpen)
            {
                IsPaneOpen = false;
            }
        }

        private void SetContentVisibility(Visibility visibility)
        {
            if (_paneToggleButtonGrid != null)
            {
                _paneToggleButtonGrid.Visibility = visibility;
            }

            if (_contentGrid != null)
            {
                _contentGrid.Visibility = visibility;
            }

            if (_paneContentGrid != null)
            {
                _paneContentGrid.Visibility = visibility;
            }
        }

        private static void OnContentVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CustomNavigationView view = (CustomNavigationView)d;
            view.SetContentVisibility((Visibility)e.NewValue);
        }

        private static void OnOverlayContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CustomNavigationView view = (CustomNavigationView)d;
            view._overlayRoot.Child = (UIElement)e.NewValue;
        }

        private static void OnOverlayZIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CustomNavigationView view = (CustomNavigationView)d;
            Canvas.SetZIndex(view._overlayRoot, (int)e.NewValue);
        }

        private static ImplicitAnimationSet GetShowAnimations()
        {
            return new ImplicitAnimationSet
            {
                new OpacityAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    From = 0,
                    To = 1
                }
            };
        }

        private static ImplicitAnimationSet GetHideAnimations()
        {
            return new ImplicitAnimationSet
            {
                new OpacityAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    From = 1,
                    To = 0
                }
            };
        }
    }
}
