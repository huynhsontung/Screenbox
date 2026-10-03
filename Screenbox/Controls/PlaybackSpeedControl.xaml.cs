using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Screenbox.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

public sealed partial class PlaybackSpeedControl : UserControl
{
    internal const double Speed025 = 0.25;
    internal const double Speed050 = 0.5;
    internal const double Speed075 = 0.75;
    internal const double Speed100 = 1.0;
    internal const double Speed125 = 1.25;
    internal const double Speed150 = 1.5;
    internal const double Speed175 = 1.75;
    internal const double Speed200 = 2.0;
    internal const double Speed400 = 4.0;

    internal PlaybackSessionViewModel PlaybackSession { get; set; }

    public PlaybackSpeedControl()
    {
        this.InitializeComponent();

        PlaybackSession = Ioc.Default.GetRequiredService<PlaybackSessionViewModel>();

        string playbackSpeed = Strings.Resources.PlaybackSpeed.ToLowerInvariant();

        ToolTipService.SetToolTip(DecreasePlaybackRateButton, Strings.Resources.DecreaseValue(playbackSpeed));
        ToolTipService.SetToolTip(IncreasePlaybackRateButton, Strings.Resources.IncreaseValue(playbackSpeed));
    }

    private void SpeedSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        double newValue = Math.Max(e.NewValue, 0.05);
        if (!DoubleHelper.AreClose(SpeedSlider.Value, newValue))
        {
            SpeedSlider.Value = newValue;
        }
    }

    private void DecreaseRateButton_OnClick(object sender, RoutedEventArgs e)
    {
        var peer = FrameworkElementAutomationPeer.FromElement(DecreasePlaybackRateButton)
            ?? FrameworkElementAutomationPeer.CreatePeerForElement(DecreasePlaybackRateButton);

        SpeedSlider.Value = Math.Max(SpeedSlider.Value - 0.05, 0.05);

        peer.RaiseNotificationEvent(
            AutomationNotificationKind.ActionCompleted,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            $"{Strings.Resources.PlaybackSpeed}: {FormatPlaybackRate(SpeedSlider.Value)}",
            $"PlaybackRateDecreaseActivityId");
    }

    private void IncreaseRateButton_OnClick(object sender, RoutedEventArgs e)
    {
        var peer = FrameworkElementAutomationPeer.FromElement(IncreasePlaybackRateButton)
            ?? FrameworkElementAutomationPeer.CreatePeerForElement(IncreasePlaybackRateButton);

        SpeedSlider.Value += 0.05;

        peer.RaiseNotificationEvent(
            AutomationNotificationKind.ActionCompleted,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            $"{Strings.Resources.PlaybackSpeed}: {FormatPlaybackRate(SpeedSlider.Value)}",
            $"PlaybackRateIncreaseActivityId");
    }

    private string FormatPlaybackRate(double playbackRate) => $"{playbackRate:0.##} ×";
}
