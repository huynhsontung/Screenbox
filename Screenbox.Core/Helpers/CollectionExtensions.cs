using CommunityToolkit.Mvvm.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Screenbox.Core.Helpers;

public static class CollectionExtensions
{
    public static void ClearItems<T>(this ICollection<T> groups) where T : IList
    {
        foreach (T group in groups)
        {
            group.Clear();
        }
    }

    public static void SyncItems<T>(this IList<T> target, IList<T> reference)
    {
        // Sync items in order. Assume items are unique
        for (int i = 0; i < reference.Count; i++)
        {
            T item = reference[i];
            if (i >= target.Count)
            {
                target.Add(item);
            }
            else
            {
                int existingIndex = target.IndexOf(item);
                if (existingIndex >= 0 && existingIndex != i)
                {
                    target.RemoveAt(existingIndex);
                }

                target.Insert(i, item);
            }
        }

        // Remove items not in "reference"
        for (int i = target.Count - 1; i >= reference.Count; i--)
        {
            target.RemoveAt(i);
        }
    }

    public static void SyncObservableGroups<TKey, TValue>(this IList<ObservableGroup<TKey, TValue>> target,
        IEnumerable<IGrouping<TKey, TValue>> reference)
    {
        Dictionary<TKey, List<TValue>> groupings = reference.ToDictionary(g => g.Key, g => g.ToList());
        foreach (ObservableGroup<TKey, TValue> observableGroup in target)
        {
            if (groupings.ContainsKey(observableGroup.Key))
            {
                observableGroup.SyncItems(groupings[observableGroup.Key]);
            }
            else
            {
                observableGroup.Clear();
            }
        }
    }
}