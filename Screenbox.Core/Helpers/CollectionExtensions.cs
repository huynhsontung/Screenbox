#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Collections;
using Screenbox.Core.ViewModels;

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

    public static void SyncItems<T>(this IList<T> target, IReadOnlyList<T> reference)
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
                if (existingIndex == i) continue;
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

    public static void SyncObservableGroups(this IList<ObservableMediaGroup> groups,
        IReadOnlyList<IGrouping<string, MediaViewModel>> reference) =>
        groups.SyncObservableGroups(reference, (key, items) =>
            items as ObservableMediaGroup ?? new ObservableMediaGroup(key, items));

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
