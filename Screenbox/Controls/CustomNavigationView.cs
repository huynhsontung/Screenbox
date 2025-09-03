#nullable enable

using System;
using System.Numerics;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

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
/// <para>The <see cref="CustomNavigationView"/> provides additional features such customizable access keys,
/// keyboard accelerators, and styles for the buttons that are built-in to NavigationView.</para>
/// It also enables changing the visibility of navigation content and rendering a custom overlay
/// with configurable z-order.
/// <para>Key features include:</para>
/// <list type="bullet">
/// <item><description><strong>Overlay:</strong> Displayed custom content on the same layer as the navigation pane.</description></item>
/// <item><description><strong>Styling:</strong> Apply custom styles to built-in buttons.</description></item>
/// <item><description><strong>Accessibility:</strong> Configure built-in buttons access keys and keyboard accelerators.</description></item>
/// <item><description><strong>Motion:</strong> Fluid animations for content when visibility changes.</description></item>
/// </list>
/// </remarks>
/// <example>
/// This example shows how to create a simple NavigationView with an overlay,
/// including some of its new capabilities.
/// <code>
/// &lt;controls:CustomNavigationView BackButtonAccessKey="B"
///                                CloseButtonStyle="{StaticResource AccentButtonStyle}"
///                                OverlayZIndex="2"&gt;
///     &lt;controls:CustomNavigationView.PaneToggleButtonKeyboardAccelerators&gt;
///         &lt;KeyboardAccelerator Key="T" Modifiers="Control" /&gt;
///     &lt;/controls:CustomNavigationView.PaneToggleButtonKeyboardAccelerators&gt;
/// 
///     &lt;controls:CustomNavigationView.Overlay&gt;
///         &lt;Border Background="Red" /&gt;
///     &lt;/controls:CustomNavigationView.Overlay&gt;
/// &lt;/controls:CustomNavigationView&gt;
/// </code>
/// </example>
public partial class CustomNavigationView : NavigationView
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
       new TranslationAnimation { From = "-48,0,0", To = "0,0,0", Duration = TimeSpan.FromMilliseconds(167), EasingMode = EasingMode.EaseInOut },
    };
    private static readonly ImplicitAnimationSet _showContentAnimationSet = new()
    {
        new OpacityAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(250), EasingType = EasingType.Linear },
        new TranslationAnimation { To = "0,0,0", Duration = TimeSpan.FromMilliseconds(500), EasingMode = EasingMode.EaseOut }
    };
    private static readonly ImplicitAnimationSet _hideContentAnimationSet = new()
    {
        new OpacityAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(167), EasingType = EasingType.Linear },
        new TranslationAnimation { To = "0,-400,0", Duration = TimeSpan.FromMilliseconds(250), EasingMode = EasingMode.EaseIn }
    };

    private Grid? _overlayRoot;
    private Border? _contentBackground;
    private SplitView? _splitView;
    private Grid? _paneToggleButtonGrid;
    private Grid? _contentGrid;
    private Grid? _paneContentGrid;
    private Button? _paneToggleButton;
    private Button? _paneSearchButton;
    private Button? _backButton;
    private Button? _closeButton;
    private NavigationViewItem? _settingsItem;

    public CustomNavigationView()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DisplayModeChanged += OnDisplayModeChanged;
        PaneOpening += OnPaneOpening;
        PaneClosing += OnPaneClosing;
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
            Implicit.SetShowAnimations(contentGrid, _showContentAnimationSet);
            Implicit.SetHideAnimations(contentGrid, _hideContentAnimationSet);
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
                if (autoSuggestBox != null)
                {
                    autoSuggestBox.Focus(FocusState.Programmatic);
                }

                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_splitView?.FindDescendant<Grid>() is { } splitViewGrid)
        {
            splitViewGrid.Children.Add(_overlayRoot);
        }

        if (_splitView?.FindDescendant<Border>(b => b.Name == "ContentBackground") is { } contentBackground)
        {
            _contentBackground = contentBackground;
            contentBackground.SetValue(Implicit.ShowAnimationsProperty, _slowFadeInAnimationSet);
            contentBackground.SetValue(Implicit.HideAnimationsProperty, _slowFadeOutAnimationSet);
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

        if (_overlayRoot != null)
        {
            _overlayRoot.Tapped -= OverlayRootOnTapped;
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

    private void OverlayRootOnTapped(object sender, TappedRoutedEventArgs e)
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

        if (_contentBackground != null)
        {
            _contentBackground.Visibility = visibility;
        }
    }

    private void UpdateOverlay()
    {
        if (_overlayRoot == null)
        {
            var overlayRoot = new Grid { Name = "OverlayRoot" };
            Grid.SetColumnSpan(overlayRoot, 2);
            overlayRoot.Tapped += OverlayRootOnTapped;

            _overlayRoot = overlayRoot;
        }

        _overlayRoot.Children.Clear();

        if (Overlay != null)
        {
            _overlayRoot.Children.Add(Overlay);
            UpdateOverlayLayout();
        }
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

        // Applies layout logic consistent with other SplitView content grids.
        if (ContentVisibility == Visibility.Collapsed)
        {
            Grid.SetColumn(_overlayRoot, 0);
            Grid.SetColumnSpan(_overlayRoot, 2);
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
}
