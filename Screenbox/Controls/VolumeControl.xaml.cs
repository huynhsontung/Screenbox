using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class VolumeControl : UserControl
    {
        public static readonly DependencyProperty ToggleButtonStyleProperty = DependencyProperty.Register(
            nameof(ToggleButtonStyle), typeof(Style), typeof(VolumeControl), new PropertyMetadata(default(Style)));

        public Style ToggleButtonStyle
        {
            get => (Style)GetValue(ToggleButtonStyleProperty);
            set => SetValue(ToggleButtonStyleProperty, value);
        }

        public static readonly DependencyProperty ShowValueTextProperty = DependencyProperty.Register(
            nameof(ShowValueText), typeof(bool), typeof(VolumeControl), new PropertyMetadata(true));

        public bool ShowValueText
        {
            get { return (bool)GetValue(ShowValueTextProperty); }
            set { SetValue(ShowValueTextProperty, value); }
        }

        internal VolumeViewModel ViewModel => (VolumeViewModel)DataContext;

        public VolumeControl()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<VolumeViewModel>();
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VolumeViewModel.MaxVolume))
            {
                UpdateIndicatorBoostWidth();
            }
        }

        private void VolumeControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateIndicatorBoostWidth();
        }

        private void UpdateIndicatorBoostWidth()
        {
            int maxVolume = ViewModel.MaxVolume;
            if (maxVolume < 100)
            {
                BoostIndicator.Width = 0;
                return;
            }

            BoostIndicator.Width = VolumeSlider.ActualWidth * (maxVolume - 100) / maxVolume;
        }
    }
}
