#nullable enable

using System;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Screenbox.Behaviors;

/// <summary>
/// Provides a behavior that automatically adjusts a <see cref="ComboBox"/> width
/// to match its dropdown popup content width when opened.
/// </summary>
/// <remarks>
/// Workaround for <a href="https://github.com/microsoft/microsoft-ui-xaml/issues/9567">microsoft/microsoft-ui-xaml#9567</a>
/// issue: <para>When the measured width of the dropdown popup content for a right-aligned <b>ComboBox</b>
/// exceeds the width of the <b>ComboBox</b> itself, the popup opens in an incorrect horizontal position.</para>
/// </remarks>
[TypeConstraint(typeof(ComboBox))]
public sealed class ComboBoxWidthFromPopupBehavior : DependencyObject, IBehavior
{
    private double? _cachedWidth;
    private Popup? _popup;

    /// <inheritdoc/>
    public DependencyObject? AssociatedObject { get; private set; }

    /// <inheritdoc/>
    public void Attach(DependencyObject associatedObject)
    {
        AssociatedObject = associatedObject;

        if (associatedObject is ComboBox comboBox)
        {
            comboBox.DropDownOpened += OnDropDownOpened;
            comboBox.DropDownClosed += OnDropDownClosed;
        }
    }

    /// <inheritdoc/>
    public void Detach()
    {
        if (AssociatedObject is ComboBox comboBox)
        {
            comboBox.DropDownOpened -= OnDropDownOpened;
            comboBox.DropDownClosed -= OnDropDownClosed;
        }

        _cachedWidth = null;
        _popup = null;
        AssociatedObject = null;
    }

    private void OnDropDownOpened(object sender, object e)
    {
        if (sender is ComboBox comboBox)
        {
            if (comboBox.ReadLocalValue(FrameworkElement.WidthProperty) != DependencyProperty.UnsetValue)
            {
                _cachedWidth = comboBox.Width;
            }

            _popup ??= FindPopup(comboBox);
            if (_popup?.Child is Canvas popupChild)
            {
                popupChild.UpdateLayout();
                double popupWidth = popupChild.ActualWidth;

                if (popupWidth > comboBox.ActualWidth)
                {
                    comboBox.Width = Math.Ceiling(popupWidth);
                }
            }
        }
    }

    private void OnDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox comboBox)
        {
            if (_cachedWidth.HasValue)
            {
                comboBox.Width = _cachedWidth.Value;
            }
            else
            {
                comboBox.ClearValue(FrameworkElement.WidthProperty);
            }
        }
    }

    private static Popup? FindPopup(ComboBox comboBox)
    {
        if (VisualTreeHelper.GetChild(comboBox, 0) is Grid layoutRootGrid)
        {
            if (layoutRootGrid.FindName("Popup") is Popup popup)
            {
                return popup;
            }
        }

        return null;
    }
}
