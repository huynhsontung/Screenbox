using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.ViewModels;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

public sealed partial class CompositeTrackPicker : UserControl
{
    public const double TimingOffsetMax = 3000d;
    public const double TimingOffsetMin = -3000d;
    public const double TimingOffsetStep = 50d;

    /// <summary>
    /// View-level subtitle track list that prepends a localized "Disable" entry to
    /// <see cref="CompositeTrackPickerViewModel.SubtitleTracks"/> and applies "Track N"
    /// fallback labels for unlabeled tracks. The subtitle ListView binds to this list.
    /// </summary>
    public ObservableCollection<string> SubtitleDisplayList { get; } = new();

    /// <summary>
    /// View-level audio track list with "Track N" fallback labels for unlabeled tracks.
    /// </summary>
    public ObservableCollection<string> AudioDisplayList { get; } = new();

    /// <summary>
    /// View-level video track list with "Track N" fallback labels for unlabeled tracks.
    /// </summary>
    public ObservableCollection<string> VideoDisplayList { get; } = new();

    internal CompositeTrackPickerViewModel ViewModel => (CompositeTrackPickerViewModel)DataContext;

    internal PlaybackSessionViewModel PlaybackSession { get; }

    public CompositeTrackPicker()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<CompositeTrackPickerViewModel>();
        PlaybackSession = Ioc.Default.GetRequiredService<PlaybackSessionViewModel>();

        ViewModel.SubtitleTracks.CollectionChanged += (_, _) => RebuildSubtitleDisplayList();
        ViewModel.AudioTracks.CollectionChanged += (_, _) => RebuildAudioDisplayList();
        ViewModel.VideoTracks.CollectionChanged += (_, _) => RebuildVideoDisplayList();
    }

    private void AddSubtitleListViewFooterItem_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        ViewModel.AddSubtitleCommand.Execute(null);
        e.Handled = true;
    }

    private void AddSubtitleListViewFooterItem_OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not (VirtualKey.Enter or VirtualKey.Space or VirtualKey.GamepadA))
            return;

        ViewModel.AddSubtitleCommand.Execute(null);
        e.Handled = true;
    }

    private void DecreaseAudioTimingOffsetButton_OnClick(object sender, RoutedEventArgs e)
    {
        AdjustTimingOffset(isAudio: true, delta: -TimingOffsetStep, notificationSource: DecreaseAudioTimingOffsetButton);
    }

    private void IncreaseAudioTimingOffsetButton_OnClick(object sender, RoutedEventArgs e)
    {
        AdjustTimingOffset(isAudio: true, delta: TimingOffsetStep, notificationSource: IncreaseAudioTimingOffsetButton);
    }

    private void DecreaseSubtitleTimingOffsetButton_OnClick(object sender, RoutedEventArgs e)
    {
        AdjustTimingOffset(isAudio: false, delta: -TimingOffsetStep, notificationSource: DecreaseSubtitleTimingOffsetButton);
    }

    private void IncreaseSubtitleTimingOffsetButton_OnClick(object sender, RoutedEventArgs e)
    {
        AdjustTimingOffset(isAudio: false, delta: TimingOffsetStep, notificationSource: IncreaseSubtitleTimingOffsetButton);
    }

    /// <summary>Formats a track's display name, falling back to "Track N" when the label is empty.</summary>
    private static string GetTrackDisplayName(string trackLabel, int oneBasedIndex) =>
        !string.IsNullOrEmpty(trackLabel)
            ? trackLabel
            : Screenbox.Strings.Resources.TrackIndex(oneBasedIndex);

    private void RebuildSubtitleDisplayList()
    {
        // Index 0 = "Disable" in the display list (maps to VM SubtitleTrackIndex = -1)
        var newList = new List<string>();
        newList.Add(Screenbox.Strings.Resources.Disable);
        for (int i = 0; i < ViewModel.SubtitleTracks.Count; i++)
        {
            newList.Add(GetTrackDisplayName(ViewModel.SubtitleTracks[i], i + 1));
        }

        // Avoid clearing and repopulating the existing ObservableCollection to prevent unexpected SelectedIndex change.
        SubtitleDisplayList.SyncItems(newList);
    }

    private void RebuildAudioDisplayList()
    {
        var newList = ViewModel.AudioTracks.Select((label, index) => GetTrackDisplayName(label, index + 1)).ToList();

        // Avoid clearing and repopulating the existing ObservableCollection to prevent unexpected SelectedIndex change.
        AudioDisplayList.SyncItems(newList);
    }

    private void RebuildVideoDisplayList()
    {
        var newList = ViewModel.VideoTracks.Select((label, index) => GetTrackDisplayName(label, index + 1)).ToList();

        // Avoid clearing and repopulating the existing ObservableCollection to prevent unexpected SelectedIndex change.
        VideoDisplayList.SyncItems(newList);
    }

    private void AdjustTimingOffset(bool isAudio, double delta, FrameworkElement notificationSource)
    {
        double currentValue = isAudio
            ? PlaybackSession.AudioTimingOffset
            : PlaybackSession.SubtitleTimingOffset;
        double newValue = Math.Clamp(currentValue + delta, TimingOffsetMin, TimingOffsetMax);

        if (isAudio)
        {
            PlaybackSession.AudioTimingOffset = newValue;
        }
        else
        {
            PlaybackSession.SubtitleTimingOffset = newValue;
        }

        string trackType = isAudio ? "Audio" : "Subtitle";
        string direction = delta > 0 ? "Increased" : "Decreased";

        var peer = FrameworkElementAutomationPeer.FromElement(notificationSource)
            ?? FrameworkElementAutomationPeer.CreatePeerForElement(notificationSource);

        peer.RaiseNotificationEvent(
            AutomationNotificationKind.ActionCompleted,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            $"{Strings.Resources.TimingOffset}: {newValue:0} ms",
            $"{trackType}TimingOffset{direction}Notification");
    }

    private bool IsTrackPickerDisplayMode(TrackPickerDisplayMode current, TrackPickerDisplayMode target)
    {
        return current == target;
    }
}
