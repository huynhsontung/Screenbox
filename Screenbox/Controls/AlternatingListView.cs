#nullable enable

using Windows.Foundation.Collections;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Screenbox.Controls
{
    public sealed class AlternatingListView : ListView
    {
        public static readonly DependencyProperty AlternateItemBackgroundProperty = DependencyProperty.Register(
            "AlternateItemBackground",
            typeof(Brush),
            typeof(AlternatingListView),
            new PropertyMetadata(null));

        public Brush? AlternateItemBackground
        {
            get => (Brush?)GetValue(AlternateItemBackgroundProperty);
            set => SetValue(AlternateItemBackgroundProperty, value);
        }

        public AlternatingListView()
        {
            //this.DefaultStyleKey = typeof(ListView);

            AlternateItemBackground = (Brush)Resources["ListViewItemBackground"];

            ContainerContentChanging += OnContainerContentChanging;
            if (Items != null)
            {
                Items.VectorChanged += ItemsOnVectorChanged;
            }
        }

        private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
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
                for (int i = (int)args.Index; i < sender.Count; i++)
                {
                    if (ContainerFromIndex(i) is SelectorItem itemContainer)
                    {
                        UpdateAlternateLayout(itemContainer, i);
                    }
                }
            }
        }

        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase > 0 || args.InRecycleQueue) return;

            args.ItemContainer.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            UpdateAlternateLayout(args.ItemContainer, args.ItemIndex);
            if (args.ItemContainer.FindDescendant<Border>() is { } border)
            {
                // Hard-coded Margin is 4,2,4,2
                border.Margin = new Thickness(0, 2, 0, 2);
            }
        }

        private void UpdateAlternateLayout(SelectorItem itemContainer, int itemIndex)
        {
            if (itemIndex < 0) return;
            Brush oddBackground = (Brush)Resources["ListViewItemBackground"];
            Brush evenBackground = AlternateItemBackground ?? oddBackground;
            itemContainer.Background = itemIndex % 2 == 0 ? evenBackground : oddBackground;

            if (itemContainer.FindDescendant<ListViewItemPresenter>() is not { } presenter) return;
            if (itemContainer.FindDescendant<Border>() is not { } border) return;
            presenter.CornerRadius = new CornerRadius(8);
            if (itemIndex % 2 == 0)
            {
                border.Background = evenBackground;
                border.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
                border.BorderThickness = new Thickness(1);
            }
            else
            {
                border.Background = oddBackground;
                border.BorderThickness = new Thickness(0);
            }
        }
    }
}
