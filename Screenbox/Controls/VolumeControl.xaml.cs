using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class VolumeControl : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="VolumeToggleButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VolumeToggleButtonStyleProperty = DependencyProperty.Register(
            nameof(VolumeToggleButtonStyle), typeof(Style), typeof(VolumeControl), new PropertyMetadata(null, OnVolumeToggleButtonStylePropertyChanged));

        /// <summary>
        /// Gets or sets the Style that defines the look of the volume toggle button.
        /// </summary>
        public Style VolumeToggleButtonStyle
        {
            get { return (Style)GetValue(VolumeToggleButtonStyleProperty); }
            set { SetValue(VolumeToggleButtonStyleProperty, value); }
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

        private static void OnVolumeToggleButtonStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VolumeControl control && control.VolumeToggleButton != null)
            {
                control.VolumeToggleButton.Style = (Style)e.NewValue;
            }
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VolumeViewModel.MaxVolume))
            {
                UpdateIndicatorBoostWidth();
            }
        }

        private void VolumeSlider_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint((UIElement)sender);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            int volumeChange = mouseWheelDelta > 0 ? 5 : -5;
            ViewModel.SetVolume(volumeChange, true);
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
