#nullable enable

using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Commands;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ViewModel.Selection.PropertyChanged += ViewModel_OnSelectionPropertyChanged;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        ViewModel.Selection.PropertyChanged -= ViewModel_OnSelectionPropertyChanged;
    }

    private void ViewModel_OnSelectionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomePageViewModel.Selection.SelectedItemCount))
        {
            if (ViewModel.Selection.SelectedItemCount == 0)
            {
                RecentFilesGridView.SelectedItems.Clear();
            }
        }
        else if (e.PropertyName == nameof(HomePageViewModel.Selection.SelectedItem))
        {
            if (ViewModel.Selection.SelectedItem is MediaViewModel item && !RecentFilesGridView.SelectedItems.Contains(item))
            {
                RecentFilesGridView.SelectedItems.Add(item);
            }
        }
    }

    private void RecentFilesGridView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.Selection.IsSelectionModeActive)
        {
            VisualStateManager.GoToState(this, "MultipleSelection", true);
        }

        ViewModel.Selection.SelectedItemCount = RecentFilesGridView.SelectedItems.Count;
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

    private void RemoveSelectedKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.Recent.Count == 0) return;

        var cmd = ViewModel.RemoveSelectedCommand;
        var parameter = RecentFilesGridView.SelectedItems;
        if (cmd.CanExecute(parameter))
        {
            cmd.Execute(parameter);
            args.Handled = true;
        }
    }
}
