#nullable enable

using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Commands;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlayQueuePage : Page
{
    internal PlayQueuePageViewModel ViewModel => (PlayQueuePageViewModel)DataContext;

    internal CommonViewModel Common { get; }

    /// <summary>
    /// Wraps <see cref="PlayQueuePageViewModel.AddFolderCommand"/> with a
    /// <see cref="NotificationCommand"/> that sends a localized error notification on failure.
    /// </summary>
    public ICommand AddFolderCommand { get; }

    public PlayQueuePage()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayQueuePageViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();

        AddFolderCommand = new NotificationCommand(
            ViewModel.AddFolderCommand,
            onFailure: e => ViewModel.SendErrorMessage(Screenbox.Strings.Resources.FailedToOpenFilesNotificationTitle, e.Message));
    }

    private async void PlayQueuePage_OnLoaded(object sender, RoutedEventArgs e)
    {
        await PlayQueue.SmoothScrollActiveItemIntoViewAsync();
    }
}
