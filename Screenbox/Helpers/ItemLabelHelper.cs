#nullable enable

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods for formatting accessible names
/// and captions for UI items.
/// </summary>
public static class ItemLabelHelper
{
    public static string GetAccessibleNameForItem(string title, double count)
    {
        string caption = Strings.Resources.ItemsCount(count);
        return $"{title}; {caption}";
    }

    public static string GetCaptionForStorageItem(bool isFile, string fileInfo, uint itemCount)
    {
        return isFile ? fileInfo : Strings.Resources.ItemsCount(itemCount);
    }

    public static string GetAccessibleNameForStorageItem(bool isFile, string name, string fileInfo, uint itemsCount)
    {
        string type = isFile ? Strings.Resources.File : Strings.Resources.Folder;
        string caption = isFile ? fileInfo : Strings.Resources.ItemsCount(itemsCount);
        return string.Concat(type, ", ", name, "; ", caption);
    }
}
