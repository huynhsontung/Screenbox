using System.Linq;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.Extensions;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class VolumeControl : UserControl
    {
        private VolumeViewModel ViewModel => (VolumeViewModel)DataContext;

        public VolumeControl()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<VolumeViewModel>();
        }

        private string GetMuteUnmuteToolTip(bool mute) =>
            $"{(mute ? Strings.Resources.UnmuteButton : Strings.Resources.MuteButton)} ({MuteButton.KeyboardAccelerators.FirstOrDefault()?.ToShortcut()})";

        private Symbol GetMuteToggleSymbol(bool isMute) => isMute ? Symbol.Mute : Symbol.Volume;
    }
}
