using Microsoft.Xaml.Interactivity;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls.Interactions
{
    internal class ThumbnailGridViewBehavior : Behavior<GridView>
    {
        private readonly IFilesService _filesService =
            CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IFilesService>();

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
                    await album.RelatedSongs[0].LoadThumbnailAsync(_filesService);
                    break;
                case MediaViewModel media:
                    await media.LoadThumbnailAsync(_filesService);
                    break;
                case StorageItemViewModel storageItem:
                    await storageItem.UpdateCaptionAsync();
                    if (storageItem.Media != null)
                    {
                        await storageItem.Media.LoadThumbnailAsync(_filesService);
                    }
                    break;
            }
        }
    }
}
