#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            DataContext = Ioc.Default.GetRequiredService<PropertyViewModel>();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Media == null) return;
            ViewModel.OnLoaded(Media);
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
