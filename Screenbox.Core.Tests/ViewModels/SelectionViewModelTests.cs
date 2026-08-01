// using System.Collections.Generic;
// using Screenbox.Core.ViewModels;
// using Windows.UI.Xaml.Data;
// using Xunit;

// namespace Screenbox.Core.Tests.ViewModels;

// public class SelectionViewModelTests
// {
//     [Fact]
//     public void SelectRange_ShouldMergeOverlappingAndContiguousRanges()
//     {
//         var vm = new SelectionViewModel();
//         vm.SelectRange(new ItemIndexRange(0, 3)); // 0, 1, 2
//         vm.SelectRange(new ItemIndexRange(2, 4)); // 2, 3, 4, 5 (overlapping)
//         vm.SelectRange(new ItemIndexRange(6, 2)); // 6, 7 (contiguous with 5)

//         Assert.Single(vm.SelectedRanges);
//         Assert.Equal(0, vm.SelectedRanges[0].FirstIndex);
//         Assert.Equal(8u, vm.SelectedRanges[0].Length); // 0..7
//         Assert.Equal(8, vm.SelectedCount);
//     }

//     [Fact]
//     public void DeselectRange_ShouldSplitRangeCorrectly()
//     {
//         var vm = new SelectionViewModel();
//         vm.SelectRange(new ItemIndexRange(0, 10)); // 0..9 (count 10)

//         // Deselect middle indices 3, 4, 5
//         vm.DeselectRange(new ItemIndexRange(3, 3));

//         Assert.Equal(2, vm.SelectedRanges.Count);
//         Assert.Equal(0, vm.SelectedRanges[0].FirstIndex);
//         Assert.Equal(3u, vm.SelectedRanges[0].Length); // 0..2

//         Assert.Equal(6, vm.SelectedRanges[1].FirstIndex);
//         Assert.Equal(4u, vm.SelectedRanges[1].Length); // 6..9

//         Assert.Equal(7, vm.SelectedCount);
//     }

//     [Fact]
//     public void SetRanges_ShouldReplaceAndCompactRanges()
//     {
//         var vm = new SelectionViewModel();
//         vm.SetRanges(new[] { new ItemIndexRange(0, 3), new ItemIndexRange(2, 4) });

//         Assert.Single(vm.SelectedRanges);
//         Assert.Equal(0, vm.SelectedRanges[0].FirstIndex);
//         Assert.Equal(6u, vm.SelectedRanges[0].Length); // 0..5
//         Assert.Equal(6, vm.SelectedCount);
//     }



//     [Fact]
//     public void GetSelectedItems_ShouldReturnCorrectItemsFromSource()
//     {
//         var source = new List<string> { "Item0", "Item1", "Item2", "Item3", "Item4", "Item5" };
//         var vm = new SelectionViewModel();
//         vm.SetItemsSource(source);

//         vm.SelectRange(new ItemIndexRange(1, 2)); // Item1, Item2
//         vm.SelectRange(new ItemIndexRange(4, 1)); // Item4

//         var selected = vm.GetSelectedItems<string>();

//         Assert.Equal(3, selected.Count);
//         Assert.Equal("Item1", selected[0]);
//         Assert.Equal("Item2", selected[1]);
//         Assert.Equal("Item4", selected[2]);
//     }

//     [Fact]
//     public void SetItemsSource_ShouldTrimRangesWhenSourceChanges()
//     {
//         var source1 = new List<string> { "Item0", "Item1", "Item2", "Item3", "Item4", "Item5" };
//         var vm = new SelectionViewModel();
//         vm.SetItemsSource(source1);
//         vm.SelectRange(new ItemIndexRange(2, 4)); // 2..5 (count 4)

//         Assert.Equal(4, vm.SelectedCount);

//         // Change source to smaller collection (size 3)
//         var source2 = new List<string> { "Item0", "Item1", "Item2" };
//         vm.SetItemsSource(source2);

//         // Range [2..5] should be trimmed to [2..2] (length 1)
//         Assert.Single(vm.SelectedRanges);
//         Assert.Equal(2, vm.SelectedRanges[0].FirstIndex);
//         Assert.Equal(1u, vm.SelectedRanges[0].Length);
//         Assert.Equal(1, vm.SelectedCount);
//     }

//     [Fact]
//     public void ClearSelection_ShouldEmptySelectedRangesAndResetState()
//     {
//         var vm = new SelectionViewModel();
//         vm.SelectRange(new ItemIndexRange(0, 2));
//         Assert.True(vm.IsSelectionModeActive);
//         Assert.NotEmpty(vm.SelectedRanges);

//         vm.ClearSelection();

//         Assert.False(vm.IsSelectionModeActive);
//         Assert.Empty(vm.SelectedRanges);
//         Assert.Equal(0, vm.SelectedCount);
//     }
// }


