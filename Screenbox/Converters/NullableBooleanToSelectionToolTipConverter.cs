#nullable enable

using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// Converts a nullable boolean value to a tooltip string for selection toggles.
/// </summary>
public sealed class NullableBooleanToSelectionToolTipConverter : IValueConverter
{
    /// <summary>
    /// Gets the tooltip text for a selection check box based on its current state.
    /// </summary>
    /// <remarks>
    /// Important: This method should not be used with <c>{x:Bind}</c> function.
    /// It will not be called when the value is <see langword="null"/>.<br/>
    /// For more information, see <see href="https://github.com/microsoft/microsoft-ui-xaml/issues/1904">
    /// microsoft-ui-xaml#1904</see>.
    /// </remarks>
    /// <param name="value">A nullable boolean representing the check box state.</param>
    /// <returns>
    /// The localized <c>SelectNoneToolTip</c> string if <paramref name="value"/> is
    /// <see langword="true"/>; otherwise, the localized <c>SelectAllToolTip</c> string.
    /// </returns>
    public static string GetSelectionToolTip(bool? value)
    {
        return value is true
            ? Strings.Resources.SelectNoneToolTip
            : Strings.Resources.SelectAllToolTip;
    }

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        bool? isChecked = (bool?)value;
        return GetSelectionToolTip(isChecked);
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
