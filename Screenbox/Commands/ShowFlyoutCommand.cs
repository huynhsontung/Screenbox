using CommunityToolkit.Mvvm.Input;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Commands;
internal class ShowFlyoutCommand : IRelayCommand
{
    public Flyout? Flyout { get; set; }

    public FrameworkElement? Target { get; set; }

    public FlyoutShowOptions ShowOptions { get; set; } = new();

    public bool CanExecute(object parameter)
    {
        return Flyout != null && Target != null;
    }

    public void Execute(object parameter)
    {
        if (Flyout == null || Target == null) return;
        Flyout.ShowAt(Target, ShowOptions);
    }

    public event EventHandler? CanExecuteChanged;
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
