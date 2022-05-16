#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Pages
{
    public class ContentPage : Page
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(ContentPage),
            new PropertyMetadata(null));

        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public bool CanGoBack { get; protected set; }

        public virtual void GoBack()
        {
        }
    }
}
