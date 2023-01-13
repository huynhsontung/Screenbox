#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Controls;

namespace Screenbox.ViewModels
{
    internal sealed partial class PlaylistViewModel : ObservableRecipient
    {
        public MediaListViewModel Playlist { get; }

        [ObservableProperty] private bool _hasItems;
        [ObservableProperty] private bool _enableMultiSelect;
        [ObservableProperty] private object? _selectedItem;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayNextCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveSelectedItemUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveSelectedItemDownCommand))]
        private int _selectionCount;

        public PlaylistViewModel(MediaListViewModel playlist)
        {
            Playlist = playlist;
            _hasItems = playlist.Items.Count > 0;
            Playlist.Items.CollectionChanged += ItemsOnCollectionChanged;
        }

        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HasItems = Playlist.Items.Count > 0;
            if (!HasItems)
            {
                EnableMultiSelect = false;
            }
        }

        partial void OnEnableMultiSelectChanged(bool value)
        {
            if (!value)
                SelectedItem = null;
        }

        public async Task EnqueueDataView(DataPackageView dataView)
        {
            if (!dataView.Contains(StandardDataFormats.StorageItems)) return;
            IReadOnlyList<IStorageItem>? items = await dataView.GetStorageItemsAsync();
            if (items?.Count > 0)
            {
                Playlist.Enqueue(items);
            }
        }

        private static bool HasSelection(IList<object>? selectedItems) => selectedItems?.Count > 0;

        private bool IsItemNotFirst(MediaViewModel item) => Playlist.Items.Count > 0 && Playlist.Items[0] != item;

        private bool IsItemNotLast(MediaViewModel item) => Playlist.Items.Count > 0 && Playlist.Items[Playlist.Items.Count - 1] != item;

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void RemoveSelected(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            List<object> copy = selectedItems.ToList();
            foreach (MediaViewModel item in copy)
            {
                Remove(item);
            }
        }

        [RelayCommand]
        private void Remove(MediaViewModel item)
        {
            if (Playlist.CurrentItem == item)
            {
                Playlist.CurrentItem = null;
            }

            Playlist.Items.Remove(item);
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void PlaySelectedNext(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            IEnumerable<object> reverse = selectedItems.Reverse();
            foreach (MediaViewModel item in reverse)
            {
                PlayNext(item);
            }
        }

        [RelayCommand]
        private void PlayNext(MediaViewModel item)
        {
            Playlist.Items.Insert(Playlist.CurrentIndex + 1, item.Clone());
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void MoveSelectedItemUp(IList<object>? selectedItems)
        {
            if (selectedItems == null || selectedItems.Count != 1) return;
            MediaViewModel item = (MediaViewModel)selectedItems[0];
            MoveItemUp(item);
            selectedItems.Add(item);
        }

        [RelayCommand(CanExecute = nameof(IsItemNotFirst))]
        private void MoveItemUp(MediaViewModel item)
        {
            int index = Playlist.Items.IndexOf(item);
            if (index <= 0) return;
            Playlist.Items.RemoveAt(index);
            Playlist.Items.Insert(index - 1, item);
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void MoveSelectedItemDown(IList<object>? selectedItems)
        {
            if (selectedItems == null || selectedItems.Count != 1) return;
            MediaViewModel item = (MediaViewModel)selectedItems[0];
            MoveItemDown(item);
            selectedItems.Add(item);
        }

        [RelayCommand(CanExecute = nameof(IsItemNotLast))]
        private void MoveItemDown(MediaViewModel item)
        {
            int index = Playlist.Items.IndexOf(item);
            if (index == -1 || index >= Playlist.Items.Count - 1) return;
            Playlist.Items.RemoveAt(index);
            Playlist.Items.Insert(index + 1, item);
        }

        [RelayCommand]
        private void ClearSelection()
        {
            EnableMultiSelect = false;
            SelectedItem = null;
        }

        [RelayCommand]
        private async Task ShowPropertiesAsync(MediaViewModel media)
        {
            ContentDialog propertiesDialog = PropertiesView.GetDialog(media);
            await propertiesDialog.ShowAsync();
        }
    }
}
