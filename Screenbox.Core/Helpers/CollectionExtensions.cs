using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Collections;

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

    /// <summary>
    /// Synchronizes <paramref name="target"/> in place to match the items and order of <paramref name="reference"/>
    /// while minimizing collection mutations.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collections.</typeparam>
    /// <param name="target">The mutable list to update in place.</param>
    /// <param name="reference">The desired sequence of items.</param>
    public static void SyncItems<T>(this IList<T> target, IReadOnlyList<T> reference)
    {
        if (ReferenceEquals(target, reference)) return;

        if (reference.Count == 0)
        {
            for (int i = target.Count - 1; i >= 0; i--)
            {
                target.RemoveAt(i);
            }

            return;
        }

        if (target.Count == 0)
        {
            for (int i = 0; i < reference.Count; i++)
            {
                target.Add(reference[i]);
            }

            return;
        }

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        // Remove items not in "reference" first so subsequent items stay in place
        // without unnecessary removals and reinsertions.
        HashSet<T> referenceSet = new(reference, comparer);
        for (int i = target.Count - 1; i >= 0; i--)
        {
            if (!referenceSet.Contains(target[i]))
            {
                target.RemoveAt(i);
            }
        }

        // Sync items in order
        for (int i = 0; i < reference.Count; i++)
        {
            T item = reference[i];
            if (i >= target.Count)
            {
                target.Add(item);
            }
            else if (!comparer.Equals(target[i], item))
            {
                // Use RemoveAt + Insert rather than ObservableCollection.Move because WinRT
                // IObservableVector has no Move action and projects Move as a full Reset.
                int existingIndex = IndexOf(target, item, i + 1, comparer);
                if (existingIndex >= 0)
                {
                    target.RemoveAt(existingIndex);
                }

                target.Insert(i, item);
            }
        }

        // Trim any leftover trailing items (e.g. excess duplicates)
        for (int i = target.Count - 1; i >= reference.Count; i--)
        {
            target.RemoveAt(i);
        }
    }

    private static int IndexOf<T>(IList<T> list, T item, int startIndex, IEqualityComparer<T> comparer)
    {
        for (int i = startIndex; i < list.Count; i++)
        {
            if (comparer.Equals(list[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    public static void SyncObservableGroups<TKey, TValue>(this IList<ObservableGroup<TKey, TValue>> target,
        IReadOnlyList<IGrouping<TKey, TValue>> reference) where TKey : notnull =>
        target.SyncObservableGroups(reference, (key, items) =>
            items as ObservableGroup<TKey, TValue> ?? new ObservableGroup<TKey, TValue>(key, items));

    public static void SyncObservableGroups<TKey, TValue, TGroup>(this IList<TGroup> target,
        IReadOnlyList<IGrouping<TKey, TValue>> reference,
        Func<TKey, IEnumerable<TValue>, TGroup> groupFactory)
        where TKey : notnull
        where TGroup : IGrouping<TKey, TValue>, IList<TValue>
    {
        var refDict = reference.ToDictionary(g => g.Key, g => g.ToList());
        var targetDict = target.ToDictionary(g => g.Key, g => g);
        var keysToSync = targetDict.Keys.Where(refDict.ContainsKey);

        // Add & Remove
        var unifiedGroups = reference.Select(g =>
                targetDict.TryGetValue(g.Key, out var targetGroup)
                    ? targetGroup
                    : groupFactory(g.Key, g))
            .ToList();
        target.SyncItems(unifiedGroups);

        // Sync
        foreach (TKey key in keysToSync)
        {
            targetDict[key].SyncItems(refDict[key]);
        }
    }
}
