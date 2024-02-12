#nullable enable

using System;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace Screenbox.Commands;
internal class BindableCommand : DependencyObject, ICommand
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(BindableCommand),
        new PropertyMetadata(default(ICommand), OnCommandChanged));

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        BindableCommand instance = (BindableCommand)d;
        if (e.OldValue is ICommand oldCommand)
        {
            oldCommand.CanExecuteChanged -= instance.OnCanExecuteChanged;
        }

        if (e.NewValue is ICommand newCommand)
        {
            newCommand.CanExecuteChanged += instance.OnCanExecuteChanged;
        }
    }

    private void OnCanExecuteChanged(object sender, EventArgs e)
    {
        CanExecuteChanged?.Invoke(this, e);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public bool CanExecute(object parameter)
    {
        return Command?.CanExecute(parameter) ?? false;
    }

    public void Execute(object parameter)
    {
        Command?.Execute(parameter);
    }

    public event EventHandler? CanExecuteChanged;
}
