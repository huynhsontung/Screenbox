using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Background
{
    public sealed class UpdateTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            try
            {
                StorageFolder local = ApplicationData.Current.LocalFolder;
                StorageFolder vlc = await local.CreateFolderAsync("vlc", CreationCollisionOption.ReplaceExisting);
                StorageFolder lua = await Package.Current.InstalledLocation.GetFolderAsync("lua");
                await CopyFolderAsync(lua, vlc);
            }
            catch (Exception)
            {
                // pass
            }
            finally
            {
                deferral.Complete();
            }
        }

        private static async Task CopyFolderAsync(IStorageFolder source, IStorageFolder destinationContainer, string desiredName = "")
        {
            if (string.IsNullOrWhiteSpace(desiredName))
            {
                desiredName = source.Name;
            }

            StorageFolder destinationFolder = await destinationContainer.CreateFolderAsync(
                desiredName, CreationCollisionOption.ReplaceExisting);

            foreach (StorageFile file in await source.GetFilesAsync())
            {
                await file.CopyAsync(destinationFolder, file.Name, NameCollisionOption.ReplaceExisting);
            }

            foreach (StorageFolder folder in await source.GetFoldersAsync())
            {
                await CopyFolderAsync(folder, destinationFolder);
            }
        }
    }
}
