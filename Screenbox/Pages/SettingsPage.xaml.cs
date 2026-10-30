using System;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.Globalization;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    private const string SystemDefaultLanguage = "LanguageSystemDefault";

    internal SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;

    internal CommonViewModel Common { get; }

    // The view model uses an empty string for "system default". Using that value directly in the
    // ComboBox would result in a blank selected item on first launch.
    private string SelectedLanguage
    {
        get => string.IsNullOrEmpty(ViewModel.Language) ? SystemDefaultLanguage : ViewModel.Language;
        set => ViewModel.Language = value.Equals(SystemDefaultLanguage) ? string.Empty : value;
    }

    private string[] VlcCommandLineHelpTextParts { get; }

    private string[] LanguageOptions => _languageOptions;

    private static readonly string[] _languageOptions = [SystemDefaultLanguage, .. ApplicationLanguages.ManifestLanguages];

    public SettingsPage()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();

        var helpText = Strings.Resources.VlcCommandLineHelpText;
        VlcCommandLineHelpTextParts = helpText.Contains("{0}")
            ? helpText.Split("{0}").Select(s => s.Trim()).Take(2).ToArray()
            : new[] { helpText, string.Empty };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadLibraryLocations();
        await ViewModel.LoadFrameCaptureFolderAsync();
        await AudioVisualSelector.ViewModel.InitializeVisualizers();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.OnNavigatedFrom();
    }

    private async void ManageSystemIndexingCard_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:search"));
    }

    [DynamicWindowsRuntimeCast(typeof(Frame))]
    private void ThemeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Window.Current.Content is Frame rootFrame)
        {
            rootFrame.RequestedTheme = ViewModel.GeneralSettings.Theme;
        }
    }

    private void LanguageComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplicationLanguages.PrimaryLanguageOverride = ViewModel.Language;
    }

    private void ChangeFrameCaptureLocationHyperlink_OnClick(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        ViewModel.ChangeFrameCaptureLocationCommand.Execute(null);
    }
}
