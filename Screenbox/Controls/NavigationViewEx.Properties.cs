#nullable enable

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls;

public sealed partial class NavigationViewEx
{
    /// <summary>
    /// Identifies the <see cref="Overlay"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty OverlayProperty = DependencyProperty.Register(
        nameof(Overlay), typeof(UIElement), typeof(NavigationViewEx), new PropertyMetadata(null, OnOverlayPropertyChanged));

    /// <summary>
    /// Gets or sets the content to be displayed as an overlay. By default, the overlay appears
    /// between the <see cref="Windows.UI.Xaml.Controls.SplitView"/> pane and the content area, but its order can be adjusted.
    /// </summary>
    public UIElement? Overlay
    {
        get => (UIElement?)GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="OverlayZIndex"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty OverlayZIndexProperty = DependencyProperty.Register(
        nameof(OverlayZIndex), typeof(int), typeof(NavigationViewEx), new PropertyMetadata(0, OnOverlayZIndexPropertyChanged));

    /// <summary>
    /// Gets or sets the Z-order of the overlay element.
    /// </summary>
    /// <value>The ZIndex value in the range ±1,000,000. The default is 0.</value>
    /// <remarks>
    /// Values above 1 will render the element above the navigation pane,
    /// and values below -1 renders the element below the main content.
    /// </remarks>
    public int OverlayZIndex
    {
        get => (int)GetValue(OverlayZIndexProperty);
        set => SetValue(OverlayZIndexProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ContentVisibility"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentVisibilityProperty = DependencyProperty.Register(
        nameof(ContentVisibility), typeof(Visibility), typeof(NavigationViewEx), new PropertyMetadata(Visibility.Visible, OnContentVisibilityPropertyChanged));

    /// <summary>
    /// Gets or sets the visibility of everything except the overlay element.
    /// </summary>
    public Visibility ContentVisibility
    {
        get => (Visibility)GetValue(ContentVisibilityProperty);
        set => SetValue(ContentVisibilityProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BackButtonAccessKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(BackButtonAccessKey), typeof(string), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the access key (mnemonic) for the back button.
    /// </summary>
    public string? BackButtonAccessKey
    {
        get => (string?)GetValue(BackButtonAccessKeyProperty);
        set => SetValue(BackButtonAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BackButtonKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackButtonKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(BackButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(new List<KeyboardAccelerator>()));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the back button.
    /// </summary>
    public IList<KeyboardAccelerator>? BackButtonKeyboardAccelerators
    {
        get => (IList<KeyboardAccelerator>?)GetValue(BackButtonKeyboardAcceleratorsProperty);
        set => SetValue(BackButtonKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BackButtonStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackButtonStyleProperty = DependencyProperty.Register(
        nameof(BackButtonStyle), typeof(Style), typeof(NavigationViewEx), new PropertyMetadata(null, OnBackButtonStylePropertyChanged));

    /// <summary>
    /// Gets or sets the style that defines the look of the back button.
    /// </summary>
    public Style? BackButtonStyle
    {
        get => (Style?)GetValue(BackButtonStyleProperty);
        set => SetValue(BackButtonStyleProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="CloseButtonAccessKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CloseButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(CloseButtonAccessKey), typeof(string), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the access key (mnemonic) for the close button.
    /// </summary>
    public string? CloseButtonAccessKey
    {
        get => (string?)GetValue(CloseButtonAccessKeyProperty);
        set => SetValue(CloseButtonAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="CloseButtonKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CloseButtonKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(CloseButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(new List<KeyboardAccelerator>()));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the close button.
    /// </summary>
    public IList<KeyboardAccelerator>? CloseButtonKeyboardAccelerators
    {
        get => (IList<KeyboardAccelerator>?)GetValue(CloseButtonKeyboardAcceleratorsProperty);
        set => SetValue(CloseButtonKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PaneToggleButtonAccessKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PaneToggleButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(PaneToggleButtonAccessKey), typeof(string), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the access key (mnemonic) for the menu toggle button.
    /// </summary>
    public string? PaneToggleButtonAccessKey
    {
        get => (string?)GetValue(PaneToggleButtonAccessKeyProperty);
        set => SetValue(PaneToggleButtonAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PaneToggleButtonKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PaneToggleButtonKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(PaneToggleButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(new List<KeyboardAccelerator>()));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the menu toggle button.
    /// </summary>
    public IList<KeyboardAccelerator>? PaneToggleButtonKeyboardAccelerators
    {
        get => (IList<KeyboardAccelerator>?)GetValue(PaneToggleButtonKeyboardAcceleratorsProperty);
        set => SetValue(PaneToggleButtonKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PaneSearchButtonAccessKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PaneSearchButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(PaneSearchButtonAccessKey), typeof(string), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the access key (mnemonic) for the search button.
    /// </summary>
    public string? PaneSearchButtonAccessKey
    {
        get => (string?)GetValue(PaneSearchButtonAccessKeyProperty);
        set => SetValue(PaneSearchButtonAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PaneSearchButtonKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PaneSearchButtonKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(PaneSearchButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(new List<KeyboardAccelerator>()));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the search button.
    /// </summary>
    public IList<KeyboardAccelerator>? PaneSearchButtonKeyboardAccelerators
    {
        get => (IList<KeyboardAccelerator>?)GetValue(PaneSearchButtonKeyboardAcceleratorsProperty);
        set => SetValue(PaneSearchButtonKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PaneSearchButtonStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PaneSearchButtonStyleProperty = DependencyProperty.Register(
        nameof(PaneSearchButtonStyle), typeof(Style), typeof(NavigationViewEx), new PropertyMetadata(null, OnPaneSearchButtonStylePropertyChanged));

    /// <summary>
    /// Gets or sets the style that defines the look of the search button.
    /// </summary>
    public Style? PaneSearchButtonStyle
    {
        get => (Style?)GetValue(PaneSearchButtonStyleProperty);
        set => SetValue(PaneSearchButtonStyleProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="SettingsItemAccessKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SettingsItemAccessKeyProperty = DependencyProperty.Register(
        nameof(SettingsItemAccessKey), typeof(string), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the access key (mnemonic) for the settings navigation view item.
    /// </summary>
    public string? SettingsItemAccessKey
    {
        get => (string?)GetValue(SettingsItemAccessKeyProperty);
        set => SetValue(SettingsItemAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="SettingsItemKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SettingsItemKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(SettingsItemKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(new List<KeyboardAccelerator>()));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the settings navigation view item.
    /// </summary>
    public IList<KeyboardAccelerator>? SettingsItemKeyboardAccelerators
    {
        get => (IList<KeyboardAccelerator>?)GetValue(SettingsItemKeyboardAcceleratorsProperty);
        set => SetValue(SettingsItemKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="SettingsItemStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SettingsItemStyleProperty = DependencyProperty.Register(
        nameof(SettingsItemStyle), typeof(Style), typeof(NavigationViewEx), new PropertyMetadata(null, OnSettingsItemStylePropertyChanged));

    /// <summary>
    /// Gets or sets the style that defines the look of the settings navigation view item.
    /// </summary>
    public Style? SettingsItemStyle
    {
        get => (Style?)GetValue(SettingsItemStyleProperty);
        set => SetValue(SettingsItemStyleProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ContentVisibilityTransition"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentVisibilityTransitionProperty = DependencyProperty.Register(
        nameof(ContentVisibilityTransition), typeof(TransitionDirection), typeof(NavigationViewEx), new PropertyMetadata(TransitionDirection.None, OnContentVisibilityTransitionPropertyChanged));

    /// <summary>
    /// Gets or sets the direction used for the translation animation of the content grid.
    /// </summary>
    public TransitionDirection ContentVisibilityTransition
    {
        get => (TransitionDirection)GetValue(ContentVisibilityTransitionProperty);
        set => SetValue(ContentVisibilityTransitionProperty, value);
    }

    private static void OnOverlayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewEx owner)
        {
            owner.OnOverlayChanged((UIElement?)e.OldValue, (UIElement?)e.NewValue);
        }
    }

    private static void OnOverlayZIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewEx owner)
        {
            owner.OnOverlayZIndexChanged((int)e.OldValue, (int)e.NewValue);
        }
    }

    private static void OnContentVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewEx owner)
        {
            owner.OnContentVisibilityChanged((Visibility)e.OldValue, (Visibility)e.NewValue);
        }
    }

    private static void OnBackButtonStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewEx owner)
        {
            owner.OnBackButtonStyleChanged((Style?)e.OldValue, (Style?)e.NewValue);
        }
    }

    private static void OnPaneSearchButtonStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewEx owner)
        {
            owner.OnPaneSearchButtonStyleChanged((Style?)e.OldValue, (Style?)e.NewValue);
        }
    }

    private static void OnSettingsItemStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewEx owner)
        {
            owner.OnSettingsItemStyleChanged((Style?)e.OldValue, (Style?)e.NewValue);
        }
    }

    private static void OnContentVisibilityTransitionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewEx owner)
        {
            owner.OnContentVisibilityTransitionChanged((TransitionDirection)e.OldValue, (TransitionDirection)e.NewValue);
        }
    }

    private void OnOverlayChanged(UIElement? oldValue, UIElement? newValue)
    {
        UpdateOverlay();
    }

    private void OnOverlayZIndexChanged(int oldValue, int newValue)
    {
        UpdateOverlayZIndex(newValue);
    }

    private void OnContentVisibilityChanged(Visibility oldValue, Visibility newValue)
    {
        UpdateContentVisibility(newValue);
        UpdateOverlayLayout();
    }

    private void OnBackButtonStyleChanged(Style? oldValue, Style? newValue)
    {
        UpdateBackButtonStyle();
        UpdateCloseButtonStyle();
    }

    private void OnPaneSearchButtonStyleChanged(Style? oldValue, Style? newValue)
    {
        UpdatePaneSearchButtonStyle();
    }

    private void OnSettingsItemStyleChanged(Style? oldValue, Style? newValue)
    {
        UpdateSettingsItemStyle();
    }

    private void OnContentVisibilityTransitionChanged(TransitionDirection oldValue, TransitionDirection newValue)
    {
        UpdateContentAnimations();
    }
}

public enum TransitionDirection
{
    None,
    Left,
    Top,
    Right,
    Bottom
}
