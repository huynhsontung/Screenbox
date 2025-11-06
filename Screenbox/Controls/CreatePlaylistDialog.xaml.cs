#nullable enable

using System;
using System.Threading.Tasks;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Controls;

public sealed partial class CreatePlaylistDialog : ContentDialog
{
    private const int MaxPlaylistNameLength = 100;

    public CreatePlaylistDialog()
    {
        this.InitializeComponent();
        FlowDirection = GlobalizationHelper.GetFlowDirection();
        RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
    }

    public static async Task<string?> GetPlaylistNameAsync()
    {
        CreatePlaylistDialog dialog = new();
        ContentDialogResult result = await dialog.ShowAsync();
        string playlistName = dialog.PlaylistNameTextBox.Text.Trim();
        return result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(playlistName)
            ? playlistName
            : null;
    }

    private void PlaylistNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        string text = PlaylistNameTextBox.Text.Trim();
        IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(text) && text.Length <= MaxPlaylistNameLength;
    }
}
