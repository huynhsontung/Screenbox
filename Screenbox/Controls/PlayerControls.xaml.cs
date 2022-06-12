#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
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

        public static readonly DependencyProperty VideoContextMenuProperty = DependencyProperty.Register(
            nameof(VideoContextMenu),
            typeof(MenuFlyout),
            typeof(PlayerControls),
            new PropertyMetadata(default(MenuFlyout)));

        public MenuFlyout VideoContextMenu
        {
            get => (MenuFlyout)GetValue(VideoContextMenuProperty);
            private set => SetValue(VideoContextMenuProperty, value);
        }

        public bool IsMinimal
        {
            get => (bool)GetValue(IsMinimalProperty);
            set => SetValue(IsMinimalProperty, value);
        }

        internal PlayerControlsViewModel ViewModel => (PlayerControlsViewModel)DataContext;

        private Flyout? _castFlyout;

        public PlayerControls()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PlayerControlsViewModel>();
            VideoContextMenu = NormalVideoContextMenu;

            VisualStateManager.GoToState(this, "Normal", false);
        }

        private void PlaybackSpeedItem_Click(object sender, RoutedEventArgs e)
        {
            RadioMenuFlyoutItem item = (RadioMenuFlyoutItem)sender;
            ViewModel.SetPlaybackSpeed(item.Text);
        }

        private void MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            _castFlyout ??= CastControl.GetFlyout();
            _castFlyout.ShowAt(MoreButton, new FlyoutShowOptions { Placement = FlyoutPlacementMode.TopEdgeAlignedRight });
        }
    }
}
