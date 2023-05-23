using System.Collections;
using System.Collections.Generic;

namespace Screenbox.Core.Helpers;

public static class GroupedCollectionExtensions
{
    public static void ClearItems<T>(this ICollection<T> groups) where T : IList
    {
        foreach (T group in groups)
        {
            group.Clear();
        }
    }
}