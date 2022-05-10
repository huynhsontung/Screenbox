using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Screenbox.Converters;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class TimeDisplay : UserControl
    {
        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
            nameof(Time),
            typeof(double),
            typeof(TimeDisplay),
            new PropertyMetadata(0d));
        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
            nameof(Length),
            typeof(double),
            typeof(TimeDisplay),
            new PropertyMetadata(0d));

        public double Time
        {
            get => (double)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public double Length
        {
            get => (double)GetValue(LengthProperty);
            set => SetValue(LengthProperty, value);
        }

        private bool _showRemaining;

        public TimeDisplay()
        {
            this.InitializeComponent();
        }

        private string GetRemainingTime(double currentTime) => HumanizedDurationConverter.Convert(currentTime - Length);

        private void TimeDisplay_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _showRemaining = !_showRemaining;
            if (_showRemaining)
            {
                RemainingText.Visibility = Visibility.Visible;
                TimeText.Visibility = Visibility.Collapsed;
            }
            else
            {
                RemainingText.Visibility = Visibility.Collapsed;
                TimeText.Visibility = Visibility.Visible;
            }
        }
    }
}
