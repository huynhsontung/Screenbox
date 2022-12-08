using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class VolumeControl : UserControl
    {
        public static readonly DependencyProperty ShowValueTextProperty = DependencyProperty.Register(
            "ShowValueText", typeof(bool), typeof(VolumeControl), new PropertyMetadata(true));

        public bool ShowValueText
        {
            get { return (bool)GetValue(ShowValueTextProperty); }
            set { SetValue(ShowValueTextProperty, value); }
        }

        internal VolumeViewModel ViewModel => (VolumeViewModel)DataContext;

        public VolumeControl()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<VolumeViewModel>();
        }

        internal string GetMuteToggleGlyph(bool isMute) => isMute ? "\uE198" : "\uE15D";
    }
}
