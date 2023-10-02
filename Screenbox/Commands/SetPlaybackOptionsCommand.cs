#nullable enable

using CommunityToolkit.Mvvm.Input;
using Screenbox.Controls;
using Screenbox.Core;
using Screenbox.Core.ViewModels;
using System;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Commands;
internal class SetPlaybackOptionsCommand : IRelayCommand
{
    public event EventHandler? CanExecuteChanged;

    public IRelayCommand? PlayCommand { get; set; }

    public bool CanExecute(object parameter)
    {
        return true;
    }

    public async void Execute(object parameter)
    {
        if (parameter is SettingsPageViewModel settings)
        {
            SetOptionsDialog dialog = new(settings.GlobalArguments, true);
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.None) return;
            settings.GlobalArguments = dialog.Options;
        }
        else
        {
            if (TryGetMedia(parameter) is not { } media) return;
            SetOptionsDialog dialog = new(string.Join(' ', media.Options));
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.None) return;
            media.SetOptions(dialog.Options);
            if (result == ContentDialogResult.Secondary)
            {
                PlayCommand?.Execute(parameter);
            }
        }
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private static MediaViewModel? TryGetMedia(object parameter) => parameter switch
    {
        MediaViewModel media => media,
        MediaViewModelWithMruToken mediaWithMru => mediaWithMru.Media,
        StorageItemViewModel storageItemViewModel => storageItemViewModel.Media,
        _ => null
    };
}
