using Microsoft.Xaml.Interactivity;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Behaviors
{
    internal class ThumbnailGridViewBehavior : Behavior<GridView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ContainerContentChanging += OnContainerContentChanging;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ContainerContentChanging -= OnContainerContentChanging;
        }

        private async void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;
            switch (args.Item)
            {
                case AlbumViewModel album:
                    await album.LoadAlbumArtAsync();
                    break;
                case MediaViewModel media:
                    await media.LoadThumbnailAsync();
                    break;
                case StorageItemViewModel storageItem:
                    await storageItem.UpdateCaptionAsync();
                    if (storageItem.Media != null)
                    {
                        await storageItem.Media.LoadThumbnailAsync();
                    }
                    break;
            }
        }
    }
}
