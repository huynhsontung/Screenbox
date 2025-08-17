#nullable enable

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls;

public partial class CustomNavigationView
{
    /// <summary>
    /// Identifies the <see cref="BackButtonAccessKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(BackButtonAccessKey), typeof(string), typeof(CustomNavigationView), new PropertyMetadata(null));

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
        nameof(BackButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(CustomNavigationView), new PropertyMetadata(new List<KeyboardAccelerator>()));

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
        nameof(BackButtonStyle), typeof(Style), typeof(CustomNavigationView), new PropertyMetadata(null, OnBackButtonStylePropertyChanged));

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
        nameof(CloseButtonAccessKey), typeof(string), typeof(CustomNavigationView), new PropertyMetadata(null));

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
        nameof(CloseButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(CustomNavigationView), new PropertyMetadata(new List<KeyboardAccelerator>()));

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
        nameof(PaneToggleButtonAccessKey), typeof(string), typeof(CustomNavigationView), new PropertyMetadata(null));

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
        nameof(PaneToggleButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(CustomNavigationView), new PropertyMetadata(new List<KeyboardAccelerator>()));

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
        nameof(PaneSearchButtonAccessKey), typeof(string), typeof(CustomNavigationView), new PropertyMetadata(null));

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
        nameof(PaneSearchButtonKeyboardAccelerators), typeof(IList<KeyboardAccelerator>), typeof(CustomNavigationView), new PropertyMetadata(new List<KeyboardAccelerator>()));

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
        nameof(PaneSearchButtonStyle), typeof(Style), typeof(CustomNavigationView), new PropertyMetadata(null, OnPaneSearchButtonStylePropertyChanged));

    /// <summary>
    /// Gets or sets the style that defines the look of the search button.
    /// </summary>
    public Style? PaneSearchButtonStyle
    {
        get => (Style?)GetValue(PaneSearchButtonStyleProperty);
        set => SetValue(PaneSearchButtonStyleProperty, value);
    }

    private static void OnBackButtonStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var owner = (CustomNavigationView)d;
        owner.UpdateBackButtonStyle();
        owner.UpdateCloseButtonStyle();
    }

    private static void OnPaneSearchButtonStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var owner = (CustomNavigationView)d;
        owner.UpdatePaneSearchButtonStyle();
    }
}
