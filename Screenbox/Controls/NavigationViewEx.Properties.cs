#nullable enable

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls;

[StyleTypedProperty(Property = nameof(BackButtonStyle), StyleTargetType = typeof(Button))]
[StyleTypedProperty(Property = nameof(PaneToggleButtonStyle), StyleTargetType = typeof(Button))]
[StyleTypedProperty(Property = nameof(PaneSearchButtonStyle), StyleTargetType = typeof(Button))]
public sealed partial class NavigationViewEx
{
    /// <summary>
    /// Identifies the <see cref="Overlay"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty OverlayProperty = DependencyProperty.Register(
        nameof(Overlay), typeof(UIElement), typeof(NavigationViewEx), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the content to be displayed as an overlay.
    /// </summary>
    /// <value>An element that contains the overlay's content. The default is <see langword="null"/>.</value>
    /// <remarks>
    /// By default, the overlay appears between the <see cref="SplitView"/> pane
    /// and the content area, the order can be adjusted with the <see cref="OverlayZIndex"/>
    /// property.</remarks>
    public UIElement? Overlay
    {
        get => (UIElement?)GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="OverlayZIndex"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty OverlayZIndexProperty = DependencyProperty.Register(
        nameof(OverlayZIndex), typeof(int), typeof(NavigationViewEx), new PropertyMetadata(0, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the Z-order of the <see cref="Overlay"/>, relative to other
    /// active regions on the screen.
    /// </summary>
    /// <value>The Z-order of the <see cref="Overlay"/>. The default is 0.</value>
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
        nameof(ContentVisibility), typeof(Visibility), typeof(NavigationViewEx), new PropertyMetadata(Visibility.Visible, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the visibility of everything except the overlay element.
    /// </summary>
    /// <value>A value of the enumeration. The default value is <b>Visible</b>.</value>
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
    /// <value>The access key (mnemonic) for the back button.</value>
    public string? BackButtonAccessKey
    {
        get => (string?)GetValue(BackButtonAccessKeyProperty);
        set => SetValue(BackButtonAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BackButtonKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackButtonKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(BackButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the back button.
    /// </summary>
    /// <value>The collection of <see cref="KeyboardAccelerator"/> objects.</value>
    public IList<KeyboardAccelerator>? BackButtonKeyboardAccelerators
    {
        get => (IList<KeyboardAccelerator>?)GetValue(BackButtonKeyboardAcceleratorsProperty);
        set => SetValue(BackButtonKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BackButtonStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackButtonStyleProperty = DependencyProperty.Register(
        nameof(BackButtonStyle), typeof(Style), typeof(NavigationViewEx), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the style that defines the look of the back button.
    /// </summary>
    /// <value>The Style that defines the look of the back button. The default is <see langword="null"/>.</value>
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
    /// <value>The access key (mnemonic) for the close button.</value>
    public string? CloseButtonAccessKey
    {
        get => (string?)GetValue(CloseButtonAccessKeyProperty);
        set => SetValue(CloseButtonAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="CloseButtonKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CloseButtonKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(CloseButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the close button.
    /// </summary>
    /// <value>The collection of <see cref="KeyboardAccelerator"/> objects.</value>
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
    /// <value>The access key (mnemonic) for the menu toggle button.</value>
    public string? PaneToggleButtonAccessKey
    {
        get => (string?)GetValue(PaneToggleButtonAccessKeyProperty);
        set => SetValue(PaneToggleButtonAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PaneToggleButtonKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PaneToggleButtonKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(PaneToggleButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the menu toggle button.
    /// </summary>
    /// <value>The collection of <see cref="KeyboardAccelerator"/> objects.</value>
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
    /// <value>The access key (mnemonic) for the search button.</value>
    public string? PaneSearchButtonAccessKey
    {
        get => (string?)GetValue(PaneSearchButtonAccessKeyProperty);
        set => SetValue(PaneSearchButtonAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PaneSearchButtonKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PaneSearchButtonKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(PaneSearchButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the search button.
    /// </summary>
    /// <value>The collection of <see cref="KeyboardAccelerator"/> objects.</value>
    public IList<KeyboardAccelerator>? PaneSearchButtonKeyboardAccelerators
    {
        get => (IList<KeyboardAccelerator>?)GetValue(PaneSearchButtonKeyboardAcceleratorsProperty);
        set => SetValue(PaneSearchButtonKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PaneSearchButtonStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PaneSearchButtonStyleProperty = DependencyProperty.Register(
        nameof(PaneSearchButtonStyle), typeof(Style), typeof(NavigationViewEx), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the style that defines the look of the search button.
    /// </summary>
    /// <value>The Style that defines the look of the search button. The default is <see langword="null"/>.</value>
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
    /// <value>The access key (mnemonic) for the settings navigation view item.</value>
    public string? SettingsItemAccessKey
    {
        get => (string?)GetValue(SettingsItemAccessKeyProperty);
        set => SetValue(SettingsItemAccessKeyProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="SettingsItemKeyboardAccelerators"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SettingsItemKeyboardAcceleratorsProperty = DependencyProperty.Register(
        nameof(SettingsItemKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(NavigationViewEx), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of keyboard combinations for the settings
    /// navigation view item.
    /// </summary>
    /// <value>The collection of <see cref="KeyboardAccelerator"/> objects.</value>
    public IList<KeyboardAccelerator>? SettingsItemKeyboardAccelerators
    {
        get => (IList<KeyboardAccelerator>?)GetValue(SettingsItemKeyboardAcceleratorsProperty);
        set => SetValue(SettingsItemKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ContentTranslationDirection"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentTranslationDirectionProperty = DependencyProperty.Register(
        nameof(ContentTranslationDirection), typeof(AnimationDirection?), typeof(NavigationViewEx), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets a value that determines which direction the content grid should
    /// translate when the animation runs.
    /// </summary>
    /// <value>A value of the enumeration. The default is <see langword="null"/>.</value>
    public AnimationDirection? ContentTranslationDirection
    {
        get => (AnimationDirection?)GetValue(ContentTranslationDirectionProperty);
        set => SetValue(ContentTranslationDirectionProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewEx owner)
        {
            owner.OnPropertyChanged(e);
        }
    }
}
