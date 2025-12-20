#nullable enable

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/CommunityToolkit/Windows/blob/v8.2.250402/components/Converters/src/ResourceNameToResourceStringConverter.cs

using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// Value converter that looks up for the source string in the App Resources strings and returns its value, if found.
/// </summary>
public sealed class ResourceNameToResourceStringConverter : IValueConverter
{
    private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

    /// <summary>
    /// Gets the localized string value for the specified resource name
    /// from the application resources.
    /// </summary>
    /// <param name="stringValue">The resource name whose value is to be retrieved.</param>
    /// <returns>The string value of the specified resoure name; otherwise; an empty string.</returns>
    public static string GetResourceString(string? stringValue)
    {
        if (stringValue is null)
        {
            return string.Empty;
        }

        // This logic was added to handle resource names prefixed with '#' or '?'.
        if (stringValue.StartsWith('#') || stringValue.StartsWith('?'))
        {
            return stringValue.Substring(1);
        }

        return _resourceLoader.GetString(stringValue);
    }

    /// <summary>
    /// Take the source string as a resource name that will be looked up in the App Resources.
    /// If the resource exists, the value is returned; otherwise, an empty string is returned.
    /// </summary>
    /// <param name="value">The source string being passed to the target.</param>
    /// <param name="targetType">The type of the target property, as a type reference.</param>
    /// <param name="parameter">Optional parameter. Not used.</param>
    /// <param name="language">The language of the conversion.</param>
    /// <returns>The string corresponding to the resource name.</returns>
    public object? Convert(object? value, Type targetType, object parameter, string language)
    {
        string? stringValue = value?.ToString();

        // The resource string logic was extracted for direct x:Bind usage.
        return GetResourceString(stringValue);
    }

    /// <summary>
    /// Not implemented.
    /// </summary>
    /// <param name="value">The source string being passed to the target.</param>
    /// <param name="targetType">The type of the target property, as a type reference.</param>
    /// <param name="parameter">Optional parameter. Not used.</param>
    /// <param name="language">The language of the conversion.</param>
    /// <returns>The value to be passed to the target dependency property.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
