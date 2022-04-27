using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class AudioTrackSubtitlePicker : UserControl
    {
        private AudioTrackSubtitleViewModel ViewModel => (AudioTrackSubtitleViewModel)DataContext;

        public AudioTrackSubtitlePicker()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<AudioTrackSubtitleViewModel>();
        }

        public static Flyout GetFlyout()
        {
            AudioTrackSubtitlePicker control = new();
            Flyout flyout = new() { Content = control };
            flyout.Opening += (_, _) => control.ViewModel.OnAudioCaptionFlyoutOpening();
            return flyout;
        }
    }
}
