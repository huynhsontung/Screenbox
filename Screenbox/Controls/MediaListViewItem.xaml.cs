#nullable enable

using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

public sealed partial class MediaListViewItem : UserControl
{
    public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
        nameof(PlayCommand), typeof(ICommand), typeof(MediaListViewItem), new PropertyMetadata(default(ICommand)));

    public ICommand PlayCommand
    {
        get => (ICommand)GetValue(PlayCommandProperty);
        set => SetValue(PlayCommandProperty, value);
    }

    public bool IsTrackNumberVisible { get; set; }

    public bool IsAlbumColumnVisible { get; set; } = true;

    public bool IsIconVisible { get; set; }

    private bool _firstPlay = true;

    private CommonViewModel Common { get; }

    public MediaListViewItem()
    {
        this.InitializeComponent();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        PlayingStates.CurrentStateChanged += PlayingStatesOnCurrentStateChanged;
    }

    private GridLength BoolToGridLength(bool visibility) =>
        visibility ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

    private void UpdatePlayButtonsAutomationName(bool isPlaying)
    {
        var media = DataContext as MediaViewModel;
        string playPauseText = isPlaying ? Strings.Resources.Pause : Strings.Resources.Play;

        AutomationProperties.SetName(PlayButton, $"{playPauseText} {media?.Name}");
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        _firstPlay = true;
        var media = DataContext as MediaViewModel;
        AdaptiveLayoutBehavior.Override = media?.MediaType != MediaPlaybackType.Music ? 0 : -1;

        UpdatePlayButtonsAutomationName(media?.IsPlaying ?? false);
        AutomationProperties.SetName(ArtistButton, $"{Strings.Resources.Artist}: {media?.MainArtist?.Name}");
        AutomationProperties.SetName(AlbumButton, $"{Strings.Resources.Albums}: {media?.Album?.Name}");
    }

    private async void PlayingStatesOnCurrentStateChanged(object sender, VisualStateChangedEventArgs e)
    {
        bool isPlaying = e.NewState?.Name == nameof(Playing);
        UpdatePlayButtonsAutomationName(isPlaying); // TODO: Use MediaViewModel PropertyChanged (IsPlaying) event.
        if (_firstPlay && isPlaying)
        {
            _firstPlay = false;
            await PlayingIndicator.PlayAsync(0, 1, true);
        }
    }
}
