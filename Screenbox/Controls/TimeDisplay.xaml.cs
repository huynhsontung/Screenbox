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
        public static readonly DependencyProperty TitleNameProperty = DependencyProperty.Register(
            nameof(TitleName),
            typeof(string),
            typeof(TimeDisplay),
            new PropertyMetadata(string.Empty, OnNameChanged));
        public static readonly DependencyProperty ChapterNameProperty = DependencyProperty.Register(
            nameof(ChapterName),
            typeof(string),
            typeof(TimeDisplay),
            new PropertyMetadata(string.Empty, OnNameChanged));
        public static readonly DependencyProperty TextBlockStyleProperty = DependencyProperty.Register(
            nameof(TextBlockStyle),
            typeof(Style),
            typeof(TimeDisplay),
            new PropertyMetadata(null));

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

        public string TitleName
        {
            get => (string)GetValue(TitleNameProperty);
            set => SetValue(TitleNameProperty, value);
        }

        public string ChapterName
        {
            get => (string)GetValue(ChapterNameProperty);
            set => SetValue(ChapterNameProperty, value);
        }

        public Style TextBlockStyle
        {
            get => (Style)GetValue(TextBlockStyleProperty);
            set => SetValue(TextBlockStyleProperty, value);
        }

        private bool _showRemaining;

        public TimeDisplay()
        {
            this.InitializeComponent();
        }

        private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeDisplay view = (TimeDisplay)d;
            if (string.IsNullOrEmpty(view.TitleName) && string.IsNullOrEmpty(view.ChapterName))
            {
                VisualStateManager.GoToState(view, "None", false);
            }
            else if (string.IsNullOrEmpty(view.TitleName) || string.IsNullOrEmpty(view.ChapterName))
            {
                VisualStateManager.GoToState(view, "Either", false);
            }
            else
            {
                VisualStateManager.GoToState(view, "Both", false);
            }
        }

        private string GetRemainingTime(double currentTime) => HumanizedDurationConverter.Convert(currentTime - Length);

        private void TimeDisplay_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _showRemaining = !_showRemaining;
            VisualStateManager.GoToState(this, _showRemaining ? "ShowRemaining" : "ShowElapsed", true);
        }
    }
}
