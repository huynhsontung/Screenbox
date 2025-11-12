#nullable enable

using System;
using System.Numerics;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;
using NavigationViewDisplayModeChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewPaneClosingEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewPaneClosingEventArgs;

namespace Screenbox.Controls;

/// <summary>
/// Represents a custom navigation view that extends the functionality of the <see cref="NavigationView"/> control.
/// </summary>
/// <remarks>
/// <para>The <see cref="NavigationViewEx"/> provides additional features such customizable access keys,
/// keyboard accelerators, and styles for the buttons that are built-in to NavigationView.</para>
/// It also supports rendering an overlay with configurable z-order and a <see cref="UIElement"/> as its content,
/// while allowing the navigation pane and main content visibility to be changed independently of the overlay.
/// <para>Key features include:</para>
/// <list type="bullet">
/// <item><description><strong>Overlay:</strong> Display custom content above the main content without obscuring the pane. The layer order can be changed.</description></item>
/// <item><description><strong>Styling:</strong> Apply custom styles to built-in buttons.</description></item>
/// <item><description><strong>Accessibility:</strong> Configure built-in buttons access keys and keyboard accelerators.</description></item>
/// <item><description><strong>Motion:</strong> Fluid animations for content when visibility changes.</description></item>
/// </list>
/// </remarks>
/// <example>
/// This example shows how to create a simple NavigationView with an overlay,
/// including some of its new capabilities.
/// <code>
/// &lt;controls:NavigationViewEx BackButtonAccessKey="B"
///                            CloseButtonStyle="{StaticResource AccentButtonStyle}"
///                            OverlayZIndex="2"&gt;
///     &lt;controls:NavigationViewEx.PaneToggleButtonKeyboardAccelerators&gt;
///         &lt;KeyboardAccelerator Key="T" Modifiers="Control" /&gt;
///     &lt;/controls:NavigationViewEx.PaneToggleButtonKeyboardAccelerators&gt;
/// 
///     &lt;controls:NavigationViewEx.Overlay&gt;
///         &lt;Border Background="Red" Height="200" /&gt;
///     &lt;/controls:NavigationViewEx.Overlay&gt;
/// &lt;/controls:NavigationViewEx&gt;
/// </code>
/// </example>
public sealed partial class NavigationViewEx : NavigationView
{
    private const string TogglePaneButtonName = "TogglePaneButton";
    private const string RootSplitViewName = "RootSplitView";
    private const string PaneContentGridName = "PaneContentGrid";
    private const string ContentGridName = "ContentGrid";
    private const string SearchButtonName = "PaneAutoSuggestButton";
    private const string PaneToggleButtonGridName = "PaneToggleButtonGrid";
    private const string NavViewBackButton = "NavigationViewBackButton";
    private const string NavViewCloseButton = "NavigationViewCloseButton";
    private const string NavViewSettingsItem = "SettingsItem";

    private const string ShadowCaster = "ShadowCaster";

    private const string ContentGridFinalValue = "400";

    private static readonly ImplicitAnimationSet _slowFadeInAnimationSet = new()
    {
       new OpacityAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(250), EasingType = EasingType.Linear }
    };
    private static readonly ImplicitAnimationSet _slowFadeOutAnimationSet = new()
    {
        new OpacityAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(250), EasingType = EasingType.Linear }
    };
    private static readonly ImplicitAnimationSet _paneShowAnimationSet = new()
    {
       new TranslationAnimation { From = "-48,0,0", To = "0,0,0", Duration = TimeSpan.FromMilliseconds(167), EasingMode = EasingMode.EaseOut },
    };
    private ImplicitAnimationSet? _contentShowAnimationSet;
    private ImplicitAnimationSet? _contentHideAnimationSet;

    private Grid? _overlayRoot;
    private Border? _overlayChildBorder;
    private Rectangle? _overlayChildRectangle;
    private SplitView? _splitView;
    private Grid? _paneToggleButtonGrid;
    private Grid? _contentGrid;
    private Grid? _paneContentGrid;
    private Button? _paneToggleButton;
    private Button? _paneSearchButton;
    private Button? _backButton;
    private Button? _closeButton;
    private NavigationViewItem? _settingsItem;

    public NavigationViewEx()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DisplayModeChanged += OnDisplayModeChanged;
        PaneOpening += OnPaneOpening;
        PaneClosing += OnPaneClosing;
        PaneClosed += OnPaneClosed;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _splitView = (SplitView?)GetTemplateChild(RootSplitViewName);

        if (GetTemplateChild(TogglePaneButtonName) is Button paneToggleButton)
        {
            _paneToggleButton = paneToggleButton;

            if (!string.IsNullOrEmpty(PaneToggleButtonAccessKey))
            {
                paneToggleButton.AccessKey = PaneToggleButtonAccessKey;
            }

            if (PaneToggleButtonKeyboardAccelerators != null)
            {
                var defaultKeyboardAccelerator = new KeyboardAccelerator
                {
                    Key = VirtualKey.Back,
                    Modifiers = VirtualKeyModifiers.Windows
                };

                // Remove the default (Windows + Back) key combination and restore it after the user-defined keyboard accelerators.
                // https://github.com/microsoft/microsoft-ui-xaml/blob/v2.8.7/dev/NavigationView/NavigationView.cpp#L407-L413
                paneToggleButton.KeyboardAccelerators.Clear();

                foreach (var item in PaneToggleButtonKeyboardAccelerators)
                {
                    paneToggleButton.KeyboardAccelerators.Add(item);
                }

                paneToggleButton.KeyboardAccelerators.Add(defaultKeyboardAccelerator);
            }
        }

        if (GetTemplateChild(SearchButtonName) is Button paneSearchButton)
        {
            _paneSearchButton = paneSearchButton;

            UpdatePaneSearchButtonStyle();

            if (!string.IsNullOrEmpty(PaneSearchButtonAccessKey))
            {
                paneSearchButton.AccessKey = PaneSearchButtonAccessKey;
            }

            if (PaneSearchButtonKeyboardAccelerators != null)
            {
                foreach (var item in PaneSearchButtonKeyboardAccelerators)
                {
                    paneSearchButton.KeyboardAccelerators.Add(item);
                    // TODO: Consolidate and add the same shortcuts to AutoSuggestBox.
                }
            }
        }

        if (GetTemplateChild(NavViewBackButton) is Button backButton)
        {
            _backButton = backButton;

            UpdateBackButtonStyle();

            if (!string.IsNullOrEmpty(BackButtonAccessKey))
            {
                backButton.AccessKey = BackButtonAccessKey;
            }

            if (BackButtonKeyboardAccelerators != null)
            {
                foreach (var item in BackButtonKeyboardAccelerators)
                {
                    backButton.KeyboardAccelerators.Add(item);
                }
            }
        }

        if (GetTemplateChild(NavViewCloseButton) is Button closeButton)
        {
            _closeButton = closeButton;

            UpdateCloseButtonStyle();

            if (!string.IsNullOrEmpty(CloseButtonAccessKey))
            {
                closeButton.AccessKey = CloseButtonAccessKey;
            }

            if (CloseButtonKeyboardAccelerators != null)
            {
                foreach (var item in CloseButtonKeyboardAccelerators)
                {
                    closeButton.KeyboardAccelerators.Add(item);
                }
            }
        }

        if (GetTemplateChild(PaneContentGridName) is Grid paneContentGrid)
        {
            _paneContentGrid = paneContentGrid;

            // Set implicit animation to play when the SplitView pane visibility changes.
            Implicit.SetShowAnimations(paneContentGrid, _paneShowAnimationSet);
            // Do not use HideAnimations, it causes the AutoSuggestBox to flicker for a few frames.
        }

        if (GetTemplateChild(PaneToggleButtonGridName) is Grid paneToggleButtonGrid)
        {
            _paneToggleButtonGrid = paneToggleButtonGrid;

            // Set implicit animations to play when ContentVisibility changes.
            Implicit.SetShowAnimations(paneToggleButtonGrid, _slowFadeInAnimationSet);
            Implicit.SetHideAnimations(paneToggleButtonGrid, _slowFadeOutAnimationSet);
        }

        if (GetTemplateChild(ContentGridName) is Grid contentGrid)
        {
            _contentGrid = contentGrid;

            // Set implicit animations to play when ContentVisibility changes.
            UpdateContentGridAnimations();
        }

        if (GetTemplateChild(ShadowCaster) is Grid shadowCaster)
        {
            shadowCaster.Translation = new Vector3(0, 0, 32);
        }

        UpdateOverlay();
        UpdateContentVisibility(ContentVisibility);
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (ContentVisibility == Visibility.Visible)
        {
            // Invoke the search experience with the gamepad Y button.
            // https://learn.microsoft.com/en-us/windows/apps/design/devices/designing-for-tv#search-experience
            // https://learn.microsoft.com/en-us/windows/apps/design/input/gamepad-and-remote-interactions#accelerator-support
            if (e.Key == VirtualKey.GamepadY && _paneSearchButton != null)
            {
                if (!IsPaneOpen)
                {
                    IsPaneOpen = true;
                }

                var autoSuggestBox = AutoSuggestBox;
                autoSuggestBox?.Focus(FocusState.Programmatic);

                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Overlay != null && _splitView?.FindDescendant<Grid>() is { } splitViewGrid)
        {
            splitViewGrid.Children.Add(_overlayRoot);
        }

        if (IsSettingsVisible && _splitView?.FindDescendant<NavigationViewItem>(s => s.Name == NavViewSettingsItem) is NavigationViewItem settingsItem)
        {
            _settingsItem = settingsItem;

            UpdateSettingsItemStyle();

            if (!string.IsNullOrEmpty(SettingsItemAccessKey))
            {
                settingsItem.AccessKey = SettingsItemAccessKey;
            }

            if (SettingsItemKeyboardAccelerators != null)
            {
                foreach (var item in SettingsItemKeyboardAccelerators)
                {
                    settingsItem.KeyboardAccelerators.Add(item);
                }
            }
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        DisplayModeChanged -= OnDisplayModeChanged;
        PaneOpening -= OnPaneOpening;
        PaneClosing -= OnPaneClosing;
        PaneClosed -= OnPaneClosed;

        if (_overlayChildRectangle != null)
        {
            _overlayChildRectangle.Tapped -= OverlayLightDismissLayer_OnTapped;
        }
    }

    private void OnDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        UpdateOverlayLayout();
    }

    private void OnPaneOpening(NavigationView sender, object args)
    {
        UpdateOverlayLayout();
    }

    private void OnPaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
    {
        UpdateOverlayLayout();
    }

    private void OnPaneClosed(NavigationView sender, object args)
    {
        UpdateOverlayLightDismissLayerVisibility();
    }

    private void OverlayLightDismissLayer_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (DisplayMode != NavigationViewDisplayMode.Expanded && IsPaneOpen)
        {
            IsPaneOpen = false;
        }
    }

    private void UpdateContentVisibility(Visibility visibility)
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

    private void UpdateOverlay()
    {
        if (Overlay == null) return;

        if (_overlayRoot == null)
        {
            var overlayRoot = new Grid { Name = "OverlayRoot" };
            Grid.SetColumnSpan(overlayRoot, 2);

            _overlayRoot = overlayRoot;

            var overlayBorder = new Border();
            _overlayChildBorder = overlayBorder;

            var rect = new Rectangle
            {
                Name = "LightDismissLayer",
                Fill = new SolidColorBrush(Colors.Transparent),
                Visibility = Visibility.Collapsed
            };
            rect.Tapped += OverlayLightDismissLayer_OnTapped;

            _overlayChildRectangle = rect;
        }

        if (_overlayChildBorder != null)
        {
            _overlayChildBorder.Child = Overlay;
        }

        _overlayRoot.Children.Clear();
        _overlayRoot.Children.Add(_overlayChildBorder);
        _overlayRoot.Children.Add(_overlayChildRectangle);
        UpdateOverlayLayout();
    }

    private void UpdateOverlayZIndex(int index)
    {
        if (_overlayRoot != null)
        {
            Canvas.SetZIndex(_overlayRoot, index);
        }
    }

    private void UpdateOverlayLayout()
    {
        if (_overlayRoot == null) return;

        // Aligns the overlay layout with the SplitView content area.
        if (ContentVisibility == Visibility.Collapsed)
        {
            Grid.SetColumn(_overlayRoot, 0);
            Grid.SetColumnSpan(_overlayRoot, 2);
            UpdateOverlayLightDismissLayerVisibility();
            return;
        }

        switch (DisplayMode)
        {
            case NavigationViewDisplayMode.Expanded:
            case NavigationViewDisplayMode.Compact:
                Grid.SetColumn(_overlayRoot, 1);
                Grid.SetColumnSpan(_overlayRoot, 1);
                break;
            case NavigationViewDisplayMode.Minimal:
            default:
                Grid.SetColumn(_overlayRoot, 0);
                Grid.SetColumnSpan(_overlayRoot, 2);
                break;
        }

        UpdateOverlayLightDismissLayerFill();
        UpdateOverlayLightDismissLayerVisibility();
    }

    private void UpdateOverlayLightDismissLayerFill()
    {
        if (_overlayChildRectangle == null) return;

        if (_splitView != null)
        {
            // We use the ContentGrid LightDismissLayer rectangle fill to avoid tracking
            // LightDismissOverlayMode, theme and high contrast changes ourselves.
            if (_splitView?.FindDescendant<Rectangle>(r => r.Name == "LightDismissLayer") is Rectangle contentRootRect)
            {
                _overlayChildRectangle.Fill = contentRootRect.Fill;
            }

            //var dismissOverlayBrush = Application.Current.Resources["SplitViewLightDismissOverlayBackground"] as SolidColorBrush; // SystemControlPageBackgroundMediumAltMediumBrush

            //_overlayChildRectangle.Fill = _splitView.LightDismissOverlayMode switch
            //{
            //    LightDismissOverlayMode.On => dismissOverlayBrush,
            //    LightDismissOverlayMode.Auto => DeviceInfoHelper.IsXbox
            //                        ? dismissOverlayBrush
            //                        : new SolidColorBrush(Colors.Transparent),
            //    _ => new SolidColorBrush(Colors.Transparent),
            //};
        }
    }

    private void UpdateOverlayLightDismissLayerVisibility()
    {
        if (_overlayChildRectangle != null)
        {
            bool showLightDismissLayer =
                (DisplayMode != NavigationViewDisplayMode.Expanded) &&
                IsPaneOpen &&
                (ContentVisibility == Visibility.Visible);

            _overlayChildRectangle.Visibility = showLightDismissLayer ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateBackButtonStyle()
    {
        if (_backButton != null)
        {
            if (BackButtonStyle != null)
            {
                _backButton.Style = BackButtonStyle;
            }
        }
    }

    private void UpdateCloseButtonStyle()
    {
        if (_closeButton != null)
        {
            if (BackButtonStyle != null)
            {
                _closeButton.Style = BackButtonStyle;
            }
        }
    }

    private void UpdatePaneSearchButtonStyle()
    {
        if (_paneSearchButton != null)
        {
            if (PaneSearchButtonStyle != null)
            {
                _paneSearchButton.Style = PaneSearchButtonStyle;
            }
        }
    }

    private void UpdateSettingsItemStyle()
    {
        if (_settingsItem != null)
        {
            if (SettingsItemStyle != null)
            {
                _settingsItem.Style = SettingsItemStyle;
            }
        }
    }

    private string GetContentAnimationTranslationTo(AnimationDirection? direction, bool isEntrance)
    {
        return direction switch
        {
            AnimationDirection.Left => isEntrance ? "0,0,0" : $"-{ContentGridFinalValue},0,0",
            AnimationDirection.Top => isEntrance ? "0,0,0" : $"0,-{ContentGridFinalValue},0",
            AnimationDirection.Right => isEntrance ? "0,0,0" : $"{ContentGridFinalValue},0,0",
            AnimationDirection.Bottom => isEntrance ? "0,0,0" : $"0,{ContentGridFinalValue},0",
            _ => "0,0,0"
        };
    }

    private void UpdateContentGridAnimations()
    {
        var showAnimationSet = new ImplicitAnimationSet
        {
            new OpacityAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(250), EasingType = EasingType.Linear }
        };
        var hideAnimationSet = new ImplicitAnimationSet
        {
            new OpacityAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(167), EasingType = EasingType.Linear }
        };

        if (ContentAnimationDirection != null)
        {
            showAnimationSet.Add(new TranslationAnimation
            {
                To = GetContentAnimationTranslationTo(ContentAnimationDirection, true),
                Duration = TimeSpan.FromMilliseconds(400),
                EasingMode = EasingMode.EaseOut
            });
            hideAnimationSet.Add(new TranslationAnimation
            {
                To = GetContentAnimationTranslationTo(ContentAnimationDirection, false),
                Duration = TimeSpan.FromMilliseconds(250),
                EasingMode = EasingMode.EaseIn
            });
        }

        _contentShowAnimationSet = showAnimationSet;
        _contentHideAnimationSet = hideAnimationSet;

        if (_contentGrid != null)
        {
            Implicit.SetShowAnimations(_contentGrid, _contentShowAnimationSet);
            Implicit.SetHideAnimations(_contentGrid, _contentHideAnimationSet);
        }
    }
}
