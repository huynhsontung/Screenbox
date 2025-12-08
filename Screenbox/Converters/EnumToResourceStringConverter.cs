#nullable enable

using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

public sealed class EnumToResourceStringConverter : IValueConverter
{
    private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

    /// <summary>
    /// Gets the localized resource string associated with the specified enum member.
    /// </summary>
    /// <param name="enumValue">The enum member to localize.</param>
    /// <returns>
    /// The resource string using the key format "{EnumType}{EnumMember}",
    /// and if not found, "EnumMember"; otherwise, an empty string.
    /// </returns>
    public static string GetResourceString(Enum? enumValue)
    {
        if (enumValue is null)
        {
            return string.Empty;
        }

        string typeName = enumValue.GetType().Name;
        string memberName = enumValue.ToString("G");
        string fullName = typeName + memberName;
        string result = _resourceLoader.GetString(fullName);

        return !string.IsNullOrEmpty(result) && result != fullName
            ? result
            : _resourceLoader.GetString(memberName);
    }

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is Enum enumValue)
        {
            return GetResourceString(enumValue);
        }

        return null;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
