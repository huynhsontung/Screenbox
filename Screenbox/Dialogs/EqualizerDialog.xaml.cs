using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Dialogs;

public sealed partial class EqualizerDialog : ContentDialog
{
    internal EqualizerViewModel ViewModel => (EqualizerViewModel)DataContext;

    public EqualizerDialog()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<EqualizerViewModel>();
        FlowDirection = GlobalizationHelper.GetFlowDirection();
        RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;

        if (!ViewModel.Presets.Contains(Strings.Resources.Custom))
        {
            ViewModel.Presets.Add(Strings.Resources.Custom);
        }
    }

    public static string FormatFrequency(float frequency)
    {
        return frequency >= 1000
            ? $"{frequency / 1000} kHz"
            : $"{frequency} Hz";
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (VisualTreeHelper.GetChild(this, 0) is Border containerBorder)
        {
            if (containerBorder.FindName("Title") is ContentControl titleControl)
            {
                titleControl.HorizontalAlignment = HorizontalAlignment.Stretch;
                titleControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            }
        }
    }
}
