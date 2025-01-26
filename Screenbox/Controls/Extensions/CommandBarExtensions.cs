using CommunityToolkit.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls.Extensions;

/// <summary>
/// Provides attached dependency properties for the <see cref="CommandBar"/> control.
/// </summary>
internal class CommandBarExtensions
{
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
            commandBar.Loaded -= ChangeCommandBarMoreButtonStyle;
            commandBar.Unloaded -= OnCommandBarUnloaded;
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
