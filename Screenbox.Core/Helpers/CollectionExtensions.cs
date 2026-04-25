#nullable enable

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

    public static void SyncObservableGroups<TKey, TValue>(this IList<ObservableGroup<TKey, TValue>> target,
        IReadOnlyList<IGrouping<TKey, TValue>> reference) where TKey : notnull
    {
        var refDict = reference.ToDictionary(g => g.Key, g => g.ToList());
        var targetDict = target.ToDictionary(g => g.Key, g => g);
        var keysToSync = targetDict.Keys.Where(refDict.ContainsKey);

        // Add & Remove
        var unifiedGroups = reference.Select(g =>
                targetDict.TryGetValue(g.Key, out var targetGroup)
                    ? targetGroup
                    : g as ObservableGroup<TKey, TValue> ?? new ObservableGroup<TKey, TValue>(g))
            .ToList();
        target.SyncItems(unifiedGroups);

        // Sync
        foreach (TKey key in keysToSync)
        {
            targetDict[key].SyncItems(refDict[key]);
        }
    }

    /// <summary>
    /// Gets the tri-state selection toggle state for a collection based on the
    /// number of selected items.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    /// <param name="source">The collection of items to evaluate.</param>
    /// <param name="selectedCount">The number of items that are currently selected.</param>
    /// <returns>
    /// <see langword="true"/> if all items in <paramref name="source"/> are selected; 
    /// <see langword="false"/> if no items are selected; otherwise, <see langword="null"/>.
    /// </returns>
    public static bool? GetSelectionToggleState<T>(this IReadOnlyCollection<T> source, int selectedCount)
    {
        if (selectedCount < 0 || selectedCount > source.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedCount));
        }

        return selectedCount == 0
            ? false
            : selectedCount == source.Count ? true : null;
    }
}
