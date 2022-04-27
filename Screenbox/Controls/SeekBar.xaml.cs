using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class SeekBar : UserControl
    {
        public static readonly DependencyProperty ProgressOnlyProperty = DependencyProperty.Register(
            nameof(ProgressOnly),
            typeof(bool),
            typeof(SeekBar),
            new PropertyMetadata(false));

        public bool ProgressOnly
        {
            get => (bool)GetValue(ProgressOnlyProperty);
            set => SetValue(ProgressOnlyProperty, value);
        }

        internal SeekBarViewModel ViewModel => (SeekBarViewModel)DataContext;

        public SeekBar()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<SeekBarViewModel>();
            RegisterSeekBarPointerHandlers();
        }

        private void RegisterSeekBarPointerHandlers()
        {
            void PointerPressedEventHandler(object s, PointerRoutedEventArgs e) => ViewModel.OnSeekBarPointerEvent(true);
            void PointerReleasedEventHandler(object s, PointerRoutedEventArgs e) => ViewModel.OnSeekBarPointerEvent(false);

            SeekBarSlider.AddHandler(PointerPressedEvent, (PointerEventHandler)PointerPressedEventHandler, true);
            SeekBarSlider.AddHandler(PointerReleasedEvent, (PointerEventHandler)PointerReleasedEventHandler, true);
            SeekBarSlider.AddHandler(PointerCanceledEvent, (PointerEventHandler)PointerReleasedEventHandler, true);
        }
    }
}
