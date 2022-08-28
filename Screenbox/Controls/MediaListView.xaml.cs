#nullable enable

using System.Collections.Generic;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class MediaListView : UserControl
    {
        public event SelectionChangedEventHandler? SelectionChanged;

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(object),
            typeof(MediaListView),
            new PropertyMetadata(null));

        public static readonly DependencyProperty ScrollBarMarginProperty = DependencyProperty.Register(
            "ScrollBarMargin",
            typeof(Thickness),
            typeof(MediaListView),
            new PropertyMetadata(default(Thickness)));

        public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
            "SelectionMode",
            typeof(ListViewSelectionMode),
            typeof(MediaListView),
            new PropertyMetadata(ListViewSelectionMode.None));

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
            "Footer",
            typeof(object),
            typeof(MediaListView),
            new PropertyMetadata(null));

        public static readonly DependencyProperty CanDragItemsProperty = DependencyProperty.Register(
            "CanDragItems",
            typeof(bool),
            typeof(MediaListView),
            new PropertyMetadata(false));

        public static readonly DependencyProperty CanReorderItemsProperty = DependencyProperty.Register(
            "CanReorderItems",
            typeof(bool),
            typeof(MediaListView),
            new PropertyMetadata(false));

        public static readonly DependencyProperty ShowMediaIconProperty = DependencyProperty.Register(
            "ShowMediaIcon",
            typeof(bool),
            typeof(MediaListView),
            new PropertyMetadata(false));

        public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
            "PlayCommand",
            typeof(ICommand),
            typeof(MediaListView),
            new PropertyMetadata(null));

        public ICommand? PlayCommand
        {
            get => (ICommand)GetValue(PlayCommandProperty);
            set => SetValue(PlayCommandProperty, value);
        }

        public bool ShowMediaIcon
        {
            get => (bool)GetValue(ShowMediaIconProperty);
            set => SetValue(ShowMediaIconProperty, value);
        }

        public bool CanReorderItems
        {
            get => (bool)GetValue(CanReorderItemsProperty);
            set => SetValue(CanReorderItemsProperty, value);
        }

        public bool CanDragItems
        {
            get => (bool)GetValue(CanDragItemsProperty);
            set => SetValue(CanDragItemsProperty, value);
        }

        public object Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        public ListViewSelectionMode SelectionMode
        {
            get => (ListViewSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public Thickness ScrollBarMargin
        {
            get => (Thickness)GetValue(ScrollBarMarginProperty);
            set => SetValue(ScrollBarMarginProperty, value);
        }

        public object ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public ItemCollection? Items => SongListView.Items;

        public IList<object> SelectedItems => SongListView.SelectedItems;

        public IReadOnlyList<ItemIndexRange> SelectedRanges => SongListView.SelectedRanges;

        public object? SelectedItem
        {
            get => SongListView.SelectedItem;
            set => SongListView.SelectedItem = value;
        }

        public int SelectedIndex
        {
            get => SongListView.SelectedIndex;
            set => SongListView.SelectedIndex = value;
        }

        private ListViewItem? _focusedItem;

        public MediaListView()
        {
            this.InitializeComponent();
        }

        public void SelectAll() => SongListView.SelectAll();

        private void SongListView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (SongListView.Items != null)
            {
                SongListView.Items.VectorChanged += SongListView_OnItemsVectorChanged;
            }
        }

        private void SongListView_OnContainerContentChanging(ListViewBase sender,
            ContainerContentChangingEventArgs args)
        {
            if (args.Phase > 0 || args.InRecycleQueue) return;

            // Remove handlers to prevent duplicated triggering
            args.ItemContainer.PointerEntered -= ItemContainerOnPointerEntered;
            args.ItemContainer.GettingFocus -= ItemContainerOnGotFocus;
            args.ItemContainer.FocusEngaged -= ItemContainerOnFocusEngaged;

            args.ItemContainer.PointerExited -= ItemContainerOnPointerExited;
            args.ItemContainer.PointerCanceled -= ItemContainerOnPointerExited;
            args.ItemContainer.LostFocus -= ItemContainerOnLostFocus;

            args.ItemContainer.DoubleTapped -= ItemContainerOnDoubleTapped;
            args.ItemContainer.SizeChanged -= ItemContainerOnSizeChanged;

            // Registering events
            args.ItemContainer.PointerEntered += ItemContainerOnPointerEntered;
            args.ItemContainer.GettingFocus += ItemContainerOnGotFocus;
            args.ItemContainer.FocusEngaged += ItemContainerOnFocusEngaged;

            args.ItemContainer.PointerExited += ItemContainerOnPointerExited;
            args.ItemContainer.PointerCanceled += ItemContainerOnPointerExited;
            args.ItemContainer.LostFocus += ItemContainerOnLostFocus;

            args.ItemContainer.DoubleTapped += ItemContainerOnDoubleTapped;
            args.ItemContainer.SizeChanged += ItemContainerOnSizeChanged;

            args.RegisterUpdateCallback(ContainerUpdateCallback);
            UpdateAlternateLayout(args.ItemContainer, args.ItemIndex);
        }

        private static async void ContainerUpdateCallback(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Item is MediaViewModel media)
            {
                await media.LoadDetailsAsync();
                if (args.ItemContainer.ContentTemplateRoot is Control control)
                {
                    UpdateDetailsLevel(control, media);
                }
            }
        }

        private static void ItemContainerOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item.ContentTemplateRoot is not Control control) return;
            if (item.Content is not MediaViewModel media) return;
            UpdateDetailsLevel(control, media);
        }

        private static void UpdateDetailsLevel(Control templateRoot, MediaViewModel media)
        {
            if (media.MusicProperties == null || media.MediaType != MediaPlaybackType.Music)
            {
                VisualStateManager.GoToState(templateRoot, "Level0", true);
                return;
            }

            if (templateRoot.ActualWidth > 800)
            {
                VisualStateManager.GoToState(templateRoot, "Level3", true);
            }
            else if (templateRoot.ActualWidth > 620)
            {
                VisualStateManager.GoToState(templateRoot, "Level2", true);
            }
            else
            {
                VisualStateManager.GoToState(templateRoot, "Level1", true);
            }
        }

        private void SongListView_OnItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
        {
            // If the index is at the end we can ignore
            if (args.Index == (sender.Count - 1))
            {
                return;
            }

            // Only need to handle Inserted and Removed because we'll handle everything else in the
            // SongListView_OnContainerContentChanging method
            if (args.CollectionChange is CollectionChange.ItemInserted or CollectionChange.ItemRemoved)
            {
                ListViewBase listViewBase = SongListView;
                for (int i = (int)args.Index; i < sender.Count; i++)
                {
                    if (listViewBase.ContainerFromIndex(i) is SelectorItem itemContainer)
                    {
                        UpdateAlternateLayout(itemContainer, i);
                    }
                }
            }
        }

        private void UpdateAlternateLayout(SelectorItem itemContainer, int itemIndex)
        {
            if (itemIndex < 0) return;
            if (itemContainer.FindDescendant<ListViewItemPresenter>() is not { } presenter) return;
            if (itemContainer.FindDescendant<Border>() is not { } border) return;
            presenter.CornerRadius = new CornerRadius(8);
            if (itemIndex % 2 == 0)
            {
                itemContainer.Background = (Brush)Resources["AccentListViewItemBackgroundBrush"];
                border.Background = (Brush)Resources["AccentListViewItemBackgroundBrush"];
                border.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
                border.BorderThickness = new Thickness(1);
            }
            else
            {
                itemContainer.Background = (Brush)Resources["ListViewItemBackground"];
                border.Background = (Brush)Resources["ListViewItemBackground"];
                border.BorderThickness = new Thickness(0);
            }
        }

        private void ItemContainerOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item.Content is MediaViewModel selectedMedia)
            {
                PlayCommand?.Execute(selectedMedia);
            }
        }

        private void ItemContainerOnFocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            sender.FindDescendant<Button>()?.Focus(FocusState.Programmatic);
        }

        private void ItemContainerOnGotFocus(object sender, RoutedEventArgs e)
        {
            _focusedItem = (ListViewItem)sender;
            ItemContainerOnPointerEntered(sender, e);
        }

        private void ItemContainerOnLostFocus(object sender, RoutedEventArgs e)
        {
            Control? control = FocusManager.GetFocusedElement() as Control;
            ListViewItem? item = control?.FindAscendantOrSelf<ListViewItem>();
            if (item == null || item != sender)
            {
                if (item == null) _focusedItem = null;
                ItemContainerOnPointerExited(sender, e);
            }
        }

        private void ItemContainerOnPointerExited(object sender, RoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item == _focusedItem) return;
            Control? control = (Control?)item.ContentTemplateRoot;
            if (control == null) return;
            VisualStateManager.GoToState(control, "Normal", false);
        }

        private void ItemContainerOnPointerEntered(object sender, RoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            Control? control = (Control?)item.ContentTemplateRoot;
            if (control == null) return;
            VisualStateManager.GoToState(control, "PointerOver", false);
        }

        private void SongListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(sender, e);
        }
    }
}