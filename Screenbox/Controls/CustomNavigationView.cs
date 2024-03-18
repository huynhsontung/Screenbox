#nullable enable

using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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

        public static readonly DependencyProperty SplitViewStyleProperty = DependencyProperty.Register(
            nameof(SplitViewStyle),
            typeof(Style),
            typeof(CustomNavigationView),
            new PropertyMetadata(null));

        public Style? SplitViewStyle
        {
            get => (Style?)GetValue(SplitViewStyleProperty);
            set => SetValue(SplitViewStyleProperty, value);
        }

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

        private Grid? _overlayRoot;
        private Border? _overlayContentHost;
        private Border? _contentBackground;
        private SplitView? _splitView;
        private Grid? _paneToggleButtonGrid;
        private Grid? _contentGrid;
        private Grid? _paneContentGrid;

        public CustomNavigationView()
        {
            Loaded += OnLoaded;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _splitView = (SplitView?)GetTemplateChild("RootSplitView");
            _paneToggleButtonGrid = (Grid?)GetTemplateChild("PaneToggleButtonGrid");
            _contentGrid = (Grid?)GetTemplateChild("ContentGrid");
            _paneContentGrid = (Grid?)GetTemplateChild("PaneContentGrid");

            if (_splitView != null)
            {
                _splitView.Style = SplitViewStyle;
            }

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

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (ContentVisibility == Visibility.Visible)
            {
                base.OnKeyDown(e);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_splitView?.FindDescendant<Grid>(b => b.Name == "OverlayRoot") is { } overlayRoot)
            {
                _overlayRoot = overlayRoot;
                overlayRoot.Tapped += OverlayRootOnTapped;
                Canvas.SetZIndex(overlayRoot, OverlayZIndex);

                if (overlayRoot.FindDescendant<Border>(b => b.Name == "OverlayContentHost") is { } overlayContentHost)
                {
                    _overlayContentHost = overlayContentHost;
                    overlayContentHost.Child = OverlayContent;
                }
            }

            if (_splitView?.FindDescendant<Border>(b => b.Name == "ContentBackground") is { } contentBackground)
            {
                _contentBackground = contentBackground;
                contentBackground.SetValue(Implicit.ShowAnimationsProperty, GetShowAnimations());
                contentBackground.SetValue(Implicit.HideAnimationsProperty, GetHideAnimations());
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

            if (_contentBackground != null)
            {
                _contentBackground.Visibility = visibility;
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
            if (view._overlayContentHost != null)
            {
                view._overlayContentHost.Child = (UIElement)e.NewValue;
            }
        }

        private static void OnOverlayZIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CustomNavigationView view = (CustomNavigationView)d;
            if (view._overlayRoot != null)
            {
                Canvas.SetZIndex(view._overlayRoot, (int)e.NewValue);
            }
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
