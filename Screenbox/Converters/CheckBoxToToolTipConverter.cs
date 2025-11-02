#nullable enable

using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// Converts a nullable boolean value to a tooltip string for selection toggles.
/// </summary>
public sealed class CheckBoxToToolTipConverter : IValueConverter
{
    /// <summary>
    /// Gets the tooltip text for a selection checkbox based on its current state.
    /// </summary>
    /// <remarks>
    /// Note: When used with <b>{x:Bind}</b> function, tooltip updates may not occur if the value is <see langword="null"/>.<br/>
    /// For more information, see <a href="https://github.com/microsoft/microsoft-ui-xaml/issues/1904">microsoft-ui-xaml#1904</a>.
    /// </remarks>
    /// <param name="value">The nullable boolean representing the checkbox state.</param>
    /// <returns>
    /// <b>SelectNoneToolTip</b> if <paramref name="value"/> is <see langword="true"/>;
    /// <b>SelectAllToolTip</b> if <paramref name="value"/> is <see langword="false"/> or <see langword="null"/>.
    /// </returns>
    public static string GetSelectionToolTip(bool? value)
    {
        return value is null
          ? Strings.Resources.SelectAllToolTip
          : (value.Value ? Strings.Resources.SelectNoneToolTip : Strings.Resources.SelectAllToolTip);
    }

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
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
