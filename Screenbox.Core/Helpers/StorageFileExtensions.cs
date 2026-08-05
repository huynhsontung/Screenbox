using System;
using Windows.Storage;

namespace Screenbox.Core.Helpers
{
    internal static class StorageFileExtensions
    {
        /// <summary>
        /// Safely compares two storage items for equality, returning <c>false</c> if
        /// <see cref="IStorageItem.IsEqual"/> throws (e.g. HRESULT 0x80070490
        /// "Element not found." when an item is in a bad or unavailable state).
        /// </summary>
        public static bool SafeIsEqual(this IStorageItem item, IStorageItem other)
        {
            try
            {
                return item.IsEqual(other);
            }
            catch (Exception)
            {
                // StorageFile.IsEqual() can throw when the underlying item is in
                // a bad state (e.g. "Element not found." HRESULT 0x80070490).
                // Treat as not equal.
                return false;
            }
        }
    }
}