using System;
using System.Linq;
using System.Numerics;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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

        private string[] VlcCommandLineHelpTextParts { get; }

        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            PendingChangesInfoBar.Translation = new Vector3(0, 0, 16);

            VlcCommandLineHelpTextParts = new string[2];
            string[] parts = Strings.Resources.VlcCommandLineHelpText
                .Split("{0}", StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToArray();
            Array.Copy(parts, VlcCommandLineHelpTextParts, VlcCommandLineHelpTextParts.Length);

            // Set the "System default" language option string
            var systemLanguageOption = ViewModel.AvailableLanguages[0];
            systemLanguageOption.NativeName = Strings.Resources.LanguageSystemDefault;
            systemLanguageOption.LayoutDirection = GlobalizationHelper.IsRightToLeftLanguage
                ? Windows.Globalization.LanguageLayoutDirection.Rtl
                : Windows.Globalization.LanguageLayoutDirection.Ltr;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.LoadLibraryLocations();
            await AudioVisualSelector.ViewModel.InitializeVisualizers();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.OnNavigatedFrom();
        }
    }
}
