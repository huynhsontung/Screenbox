#nullable enable

using System;
using System.Threading.Tasks;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

public sealed partial class RenamePlaylistDialog : ContentDialog
{
    private const int MaxPlaylistNameLength = 100;

    public RenamePlaylistDialog(string currentName)
    {
        this.DefaultStyleKey = typeof(ContentDialog);
        this.InitializeComponent();
        FlowDirection = GlobalizationHelper.GetFlowDirection();
        RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
        PlaylistNameTextBox.Text = currentName;
        PlaylistNameTextBox.SelectAll();
    }

    public async Task<string?> GetPlaylistNameAsync()
    {
        ContentDialogResult result = await ShowAsync();
        string playlistName = PlaylistNameTextBox.Text.Trim();
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
