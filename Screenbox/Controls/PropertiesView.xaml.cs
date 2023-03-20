#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.Core.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PropertiesView : UserControl
    {
        public static readonly DependencyProperty MediaProperty = DependencyProperty.Register(
            nameof(Media),
            typeof(MediaViewModel),
            typeof(PropertiesView),
            new PropertyMetadata(null, OnMediaChanged));

        internal MediaViewModel? Media
        {
            get => (MediaViewModel?)GetValue(MediaProperty);
            set => SetValue(MediaProperty, value);
        }

        internal PropertyViewModel ViewModel => (PropertyViewModel)DataContext;

        public PropertiesView()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PropertyViewModel>();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Media == null) return;
            await Media.LoadDetailsAsync();
        }

        private static void OnMediaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertiesView view = (PropertiesView)d;
            MediaViewModel? media = (MediaViewModel?)e.NewValue;
            if (media == null) return;

            view.ViewModel.UpdateProperties(media);
        }
    }
}
