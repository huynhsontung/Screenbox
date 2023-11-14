#nullable enable

using Screenbox.Core.ViewModels;
using System;
using Windows.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;
public sealed partial class MediaListViewItem : UserControl
{
    public bool IsTrackNumberVisible { get; set; }

    public bool IsAlbumColumnVisible { get; set; } = true;

    public bool IsIconVisible { get; set; }

    private bool _firstPlay = true;

    public MediaListViewItem()
    {
        this.InitializeComponent();
        PlayingStates.CurrentStateChanged += PlayingStatesOnCurrentStateChanged;
    }

    private GridLength BoolToGridLength(bool visibility) =>
        visibility ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        _firstPlay = true;
        AdaptiveLayoutBehavior.Override =
            (DataContext as MediaViewModel)?.MediaType != MediaPlaybackType.Music ? 0 : -1;
    }

    private async void PlayingStatesOnCurrentStateChanged(object sender, VisualStateChangedEventArgs e)
    {
        if (_firstPlay && e.NewState?.Name == nameof(Playing))
        {
            _firstPlay = false;
            await PlayingIndicator.PlayAsync(0, 1, true);
        }
    }
}
