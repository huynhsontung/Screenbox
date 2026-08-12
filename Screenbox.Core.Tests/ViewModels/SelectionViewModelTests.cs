using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Data;

namespace Screenbox.Core.Tests.ViewModels;

public class SelectionViewModelTests
{
    [Test]
    public async Task SelectRange_ShouldMergeOverlappingAndContiguousRanges()
    {
        var vm = new SelectionViewModel();
        vm.SelectRange(new ItemIndexRange(0, 3)); // 0, 1, 2
        vm.SelectRange(new ItemIndexRange(2, 4)); // 2, 3, 4, 5 (overlapping)
        vm.SelectRange(new ItemIndexRange(6, 2)); // 6, 7 (contiguous with 5)

        await Assert.That(vm.SelectedRanges).HasSingleItem();
        await Assert.That(vm.SelectedRanges[0].FirstIndex).IsEqualTo(0);
        await Assert.That(vm.SelectedRanges[0].Length).IsEqualTo(8u); // 0..7
        await Assert.That(vm.SelectedCount).IsEqualTo(8);
    }

    [Test]
    public async Task DeselectRange_ShouldSplitRangeCorrectly()
    {
        var vm = new SelectionViewModel();
        vm.SelectRange(new ItemIndexRange(0, 10)); // 0..9 (count 10)

        // Deselect middle indices 3, 4, 5
        vm.DeselectRange(new ItemIndexRange(3, 3));

        await Assert.That(vm.SelectedRanges.Count).IsEqualTo(2);
        await Assert.That(vm.SelectedRanges[0].FirstIndex).IsEqualTo(0);
        await Assert.That(vm.SelectedRanges[0].Length).IsEqualTo(3u); // 0..2

        await Assert.That(vm.SelectedRanges[1].FirstIndex).IsEqualTo(6);
        await Assert.That(vm.SelectedRanges[1].Length).IsEqualTo(4u); // 6..9

        await Assert.That(vm.SelectedCount).IsEqualTo(7);
    }

    [Test]
    public async Task SetRanges_ShouldReplaceAndCompactRanges()
    {
        var vm = new SelectionViewModel();
        vm.SetRanges(new[] { new ItemIndexRange(0, 3), new ItemIndexRange(2, 4) });

        await Assert.That(vm.SelectedRanges).HasSingleItem();
        await Assert.That(vm.SelectedRanges[0].FirstIndex).IsEqualTo(0);
        await Assert.That(vm.SelectedRanges[0].Length).IsEqualTo(6u); // 0..5
        await Assert.That(vm.SelectedCount).IsEqualTo(6);
    }



    [Test]
    public async Task GetSelectedItems_ShouldReturnCorrectItemsFromSource()
    {
        var source = new List<string> { "Item0", "Item1", "Item2", "Item3", "Item4", "Item5" };
        var vm = new SelectionViewModel();
        vm.SetItemsSource(source);

        vm.SelectRange(new ItemIndexRange(1, 2)); // Item1, Item2
        vm.SelectRange(new ItemIndexRange(4, 1)); // Item4

        var selected = vm.GetSelectedItems<string>();

        await Assert.That(selected.Count).IsEqualTo(3);
        await Assert.That(selected[0]).IsEqualTo("Item1");
        await Assert.That(selected[1]).IsEqualTo("Item2");
        await Assert.That(selected[2]).IsEqualTo("Item4");
    }

    [Test]
    public async Task SetItemsSource_ShouldTrimRangesWhenSourceChanges()
    {
        var source1 = new List<string> { "Item0", "Item1", "Item2", "Item3", "Item4", "Item5" };
        var vm = new SelectionViewModel();
        vm.SetItemsSource(source1);
        vm.SelectRange(new ItemIndexRange(2, 4)); // 2..5 (count 4)

        await Assert.That(vm.SelectedCount).IsEqualTo(4);

        // Change source to smaller collection (size 3)
        var source2 = new List<string> { "Item0", "Item1", "Item2" };
        vm.SetItemsSource(source2);

        // Range [2..5] should be trimmed to [2..2] (length 1)
        await Assert.That(vm.SelectedRanges).HasSingleItem();
        await Assert.That(vm.SelectedRanges[0].FirstIndex).IsEqualTo(2);
        await Assert.That(vm.SelectedRanges[0].Length).IsEqualTo(1u);
        await Assert.That(vm.SelectedCount).IsEqualTo(1);
    }

    [Test]
    public async Task ClearSelection_ShouldEmptySelectedRanges()
    {
        var vm = new SelectionViewModel();
        vm.SelectRange(new ItemIndexRange(0, 2));
        await Assert.That(vm.IsSelectionModeActive).IsTrue();
        await Assert.That(vm.SelectedRanges).IsNotEmpty();

        vm.ClearSelection();

        await Assert.That(vm.IsSelectionModeActive).IsTrue();
        await Assert.That(vm.SelectedRanges).IsEmpty();
        await Assert.That(vm.SelectedCount).IsEqualTo(0);
    }
}
