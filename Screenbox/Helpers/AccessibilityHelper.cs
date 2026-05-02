#nullable enable

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods for formatting accessible
/// names and captions for UI elements.
/// </summary>
public static class AccessibilityHelper
{
    public static string FormatAccessibleName(string title, string? caption)
    {
        return caption is null or ""
            ? title
            : $"{title}; {caption}";
    }

    public static string FormatAccessibleName(string title, double count)
    {
        var caption = Strings.Resources.ItemsCount(count);
        return $"{title}; {caption}";
    }

    public static string GetStorageItemCaption(bool isFile, string fileInfo, uint itemCount)
    {
        return isFile ? fileInfo : Strings.Resources.ItemsCount(itemCount);
    }

    public static string GetStorageItemAccessibleName(bool isFile, string name, string fileInfo, uint itemsCount)
    {
        var caption = GetStorageItemCaption(isFile, fileInfo, itemsCount);
        return FormatAccessibleName(name, caption);
    }
}
