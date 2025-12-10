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
    private Popup? _popup;
    private double? _cachedPopupWidth;

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

        _popup = null;
        _cachedPopupWidth = null;
        AssociatedObject = null;
    }

    private void OnDropDownOpened(object sender, object e)
    {
        if (sender is ComboBox comboBox)
        {
            if (_popup is null)
            {
                _popup = FindPopup();
            }

            if (_popup?.Child is FrameworkElement popupChild)
            {
                if (_cachedPopupWidth.HasValue)
                {
                    comboBox.Width = _cachedPopupWidth.Value;
                }
                else
                {
                    popupChild.UpdateLayout();
                    double popupWidth = popupChild.ActualWidth;

                    if (popupWidth > comboBox.ActualWidth)
                    {
                        _cachedPopupWidth = Math.Ceiling(popupWidth);
                        comboBox.Width = _cachedPopupWidth.Value;
                    }
                }
            }
        }
    }

    private void OnDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox comboBox)
        {
            comboBox.Width = double.NaN;
        }
    }

    private static Popup? FindPopup()
    {
        if (Window.Current is null)
        {
            return null;
        }

        var openPopups = VisualTreeHelper.GetOpenPopups(Window.Current);
        return openPopups.Count == 0 ? null : openPopups[0];
    }
}
