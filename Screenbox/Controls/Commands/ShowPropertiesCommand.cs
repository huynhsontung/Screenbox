#nullable enable

using System;
using System.Windows.Input;
using Screenbox.Core;
using Screenbox.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls.Commands
{
    internal class ShowPropertiesCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return parameter is MediaViewModel or StorageItemViewModel or MediaViewModelWithMruToken;
        }

        public async void Execute(object parameter)
        {
            MediaViewModel? media = null;
            switch (parameter)
            {
                case MediaViewModel m:
                    media = m;
                    break;
                case StorageItemViewModel item:
                    media = item.Media;
                    break;
                case MediaViewModelWithMruToken mru:
                    media = mru.Media;
                    break;
            }

            if (media == null) return;
            ContentDialog propertiesDialog = PropertiesView.GetDialog(media);
            await propertiesDialog.ShowAsync();
        }

        public event EventHandler? CanExecuteChanged;
    }
}
