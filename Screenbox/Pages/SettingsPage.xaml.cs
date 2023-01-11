using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        internal SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;

        internal CommonViewModel Common { get; }
        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<SettingsPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        private void ContentRoot_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            StackPanel panel = (StackPanel)sender;
            ButtonBase? settingsCard = panel.Children.OfType<ButtonBase>().FirstOrDefault();
            if (settingsCard == null) return;
            IEnumerable<TextBlock> sectionHeaders = panel.Children.OfType<TextBlock>();
            foreach (TextBlock sectionHeader in sectionHeaders)
            {
                sectionHeader.Width = settingsCard.ActualWidth;
            }
        }
    }
}
