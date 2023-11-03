using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class AudioTrackSubtitlePicker : UserControl
    {
        public IRelayCommand? ShowAudioOptionsCommand { get; set; }

        public IRelayCommand? ShowSubtitleOptionsCommand { get; set; }

        internal AudioTrackSubtitleViewModel ViewModel => (AudioTrackSubtitleViewModel)DataContext;

        public AudioTrackSubtitlePicker()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<AudioTrackSubtitleViewModel>();
        }
    }
}
