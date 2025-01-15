using CommunityToolkit.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls.Extensions;

/// <summary>
/// Provides attached dependency properties for the <see cref="CommandBar"/> control.
/// </summary>
internal class CommandBarExtensions
{
    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding a <see cref="KeyboardAccelerator"/> to the more <see cref="Button"/> of the associated <see cref="CommandBar"/>
    /// </summary>
    public static readonly DependencyProperty MoreButtonKeyboardAcceleratorsProperty = DependencyProperty.RegisterAttached(
        "MoreButtonKeyboardAccelerators", typeof(KeyboardAccelerator), typeof(CommandBarExtensions), new PropertyMetadata(null, OnMoreButtonKeyboardAcceleratorsPropertyChanged));

    /// <summary>
    /// Gets the <see cref="KeyboardAccelerator"/> for the more <see cref="Button"/> of the associated <see cref="CommandBar"/>
    /// </summary>
    /// <returns>The <see cref="KeyboardAccelerator"/> associated with the <see cref="CommandBar"/> more <see cref="Button"/>.</returns>
    public static KeyboardAccelerator GetMoreButtonKeyboardAccelerators(CommandBar obj)
    {
        return (KeyboardAccelerator)obj.GetValue(MoreButtonKeyboardAcceleratorsProperty);
    }

    /// <summary>
    /// Sets the <see cref="KeyboardAccelerator"/> to the more <see cref="Button"/> of the associated <see cref="CommandBar"/>
    /// </summary>
    public static void SetMoreButtonKeyboardAccelerators(CommandBar obj, KeyboardAccelerator value)
    {
        obj.SetValue(MoreButtonKeyboardAcceleratorsProperty, value);
    }

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding a <see cref="Style"/> to the more <see cref="Button"/> of the associated <see cref="CommandBar"/>
    /// </summary>
    public static readonly DependencyProperty MoreButtonStyleProperty = DependencyProperty.RegisterAttached(
        "MoreButtonStyle", typeof(Style), typeof(CommandBarExtensions), new PropertyMetadata(null, OnMoreButtonStylePropertyChanged));

    /// <summary>
    /// Gets the <see cref="Style"/> for the more <see cref="Button"/> of the associated <see cref="CommandBar"/>
    /// </summary>
    /// <returns>The <see cref="Style"/> associated with the <see cref="CommandBar"/> more <see cref="Button"/>.</returns>
    public static Style GetMoreButtonStyle(CommandBar obj)
    {
        return (Style)obj.GetValue(MoreButtonStyleProperty);
    }

    /// <summary>
    /// Sets the <see cref="Style"/> to the more <see cref="Button"/> of the associated <see cref="CommandBar"/>
    /// </summary>
    public static void SetMoreButtonStyle(CommandBar obj, Style value)
    {
        obj.SetValue(MoreButtonStyleProperty, value);
    }

    private static void OnCommandBarUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is CommandBar commandBar)
        {
            commandBar.Loaded -= ChangeCommandBarMoreButtonKeyboardAccelerators;
            commandBar.Loaded -= ChangeCommandBarMoreButtonStyle;
            commandBar.Unloaded -= OnCommandBarUnloaded;
        }
    }

    private static void OnMoreButtonKeyboardAcceleratorsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is CommandBar commandBar)
        {
            commandBar.Loaded -= ChangeCommandBarMoreButtonKeyboardAccelerators;
            commandBar.Unloaded -= OnCommandBarUnloaded;

            if (MoreButtonKeyboardAcceleratorsProperty != null)
            {
                commandBar.Loaded += ChangeCommandBarMoreButtonKeyboardAccelerators;
                commandBar.Unloaded += OnCommandBarUnloaded;
            }
        }
    }

    private static void ChangeCommandBarMoreButtonKeyboardAccelerators(object sender, RoutedEventArgs args)
    {
        if (sender is CommandBar commandBar)
        {
            Button moreButton = commandBar.FindDescendant<Button>(b => b.Name == "MoreButton");
            if (moreButton != null)
            {
                moreButton.KeyboardAccelerators.Add(GetMoreButtonKeyboardAccelerators(commandBar));
            }
        }
    }

    private static void OnMoreButtonStylePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is CommandBar commandBar)
        {
            commandBar.Loaded -= ChangeCommandBarMoreButtonStyle;
            commandBar.Unloaded -= OnCommandBarUnloaded;

            if (MoreButtonStyleProperty != null)
            {
                commandBar.Loaded += ChangeCommandBarMoreButtonStyle;
                commandBar.Unloaded += OnCommandBarUnloaded;
            }
        }
    }

    private static void ChangeCommandBarMoreButtonStyle(object sender, RoutedEventArgs args)
    {
        if (sender is CommandBar commandBar)
        {
            Button moreButton = commandBar.FindDescendant<Button>(b => b.Name == "MoreButton");
            if (moreButton != null)
            {
                moreButton.Style = GetMoreButtonStyle(commandBar);
            }
        }
    }
}
