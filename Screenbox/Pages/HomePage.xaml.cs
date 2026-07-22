#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Commands;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    internal HomePageViewModel ViewModel => (HomePageViewModel)DataContext;

    internal CommonViewModel Common { get; }

    private readonly SelectDeselectAllCommand _selectionCommand;

    public HomePage()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<HomePageViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        _selectionCommand = new SelectDeselectAllCommand();
    }

    private void SelectDeselectAllKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.Recent.Count > 0 && _selectionCommand.CanToggleSelection(RecentFilesGridView))
        {
            ViewModel.Selection.IsSelectionModeActive = true;
            _selectionCommand.ToggleSelection(RecentFilesGridView);
            args.Handled = true;
        }
    }
}
