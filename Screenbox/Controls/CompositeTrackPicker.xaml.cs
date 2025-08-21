using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class CompositeTrackPicker : UserControl
    {
        public IRelayCommand? ShowSubtitleOptionsCommand { get; set; }
        public IRelayCommand? ShowAudioOptionsCommand { get; set; }

        internal CompositeTrackPickerViewModel ViewModel => (CompositeTrackPickerViewModel)DataContext;

        public CompositeTrackPicker()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<CompositeTrackPickerViewModel>();
        }
    }
}
