#nullable enable

using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicPage : ContentPage
    {
        internal MusicPageViewModel ViewModel => (MusicPageViewModel)DataContext;

        private ListViewItem? _focusedItem;

        public MusicPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<MusicPageViewModel>();
        }

        private async void MusicPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Fetching", true);
            await ViewModel.FetchSongsAsync();
            SongsSource.Source = ViewModel.GroupedSongs;
            VisualStateManager.GoToState(this, ViewModel.GroupedSongs?.Count > 0 ? "Normal" : "NoContent", true);
            if (SongListView.Items != null)
            {
                SongListView.Items.VectorChanged += SongListView_OnItemsVectorChanged;
            }
        }

        private void SongListView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
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

            // There is no lightweight styling for ListViewItem padding
            args.ItemContainer.Padding = new Thickness(0);
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
            if (media.MusicProperties == null)
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
            if (itemContainer.ContentTemplateRoot is not UserControl templateRoot) return;
            if (templateRoot.Content is not Grid control) return;
            if (itemIndex % 2 == 0)
            {
                control.Background = (Brush)Resources["SystemControlBackgroundListLowBrush"];
                control.Background.Opacity = 0.4;
                control.BorderThickness = new Thickness(1);
            }
            else
            {
                control.Background = null;
                control.BorderThickness = new Thickness(0);
            }
        }

        private void ItemContainerOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item.Content is MediaViewModel selectedMedia)
            {
                ViewModel.PlayCommand.Execute(selectedMedia);
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
    }
}
