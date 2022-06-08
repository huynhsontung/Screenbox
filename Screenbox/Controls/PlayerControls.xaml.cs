using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PlayerControls : UserControl
    {
        public static readonly DependencyProperty IsMinimalProperty = DependencyProperty.Register(
            nameof(IsMinimal),
            typeof(bool),
            typeof(PlayerControls),
            new PropertyMetadata(false));

        public bool IsMinimal
        {
            get { return (bool)GetValue(IsMinimalProperty); }
            set { SetValue(IsMinimalProperty, value); }
        }

        internal PlayerControlsViewModel ViewModel => (PlayerControlsViewModel)DataContext;

        public PlayerControls()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PlayerControlsViewModel>();

            VisualStateManager.GoToState(this, "Normal", false);
        }
    }
}
