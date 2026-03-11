#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Helpers;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

public sealed partial class CompositeTrackPicker : UserControl
{
    public IRelayCommand? ShowSubtitleOptionsCommand { get; set; }
    public IRelayCommand? ShowAudioOptionsCommand { get; set; }

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

    public CompositeTrackPicker()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<CompositeTrackPickerViewModel>();

        ViewModel.SubtitleTracks.CollectionChanged += (_, _) => RebuildSubtitleDisplayList();
        ViewModel.AudioTracks.CollectionChanged += (_, _) => RebuildAudioDisplayList();
        ViewModel.VideoTracks.CollectionChanged += (_, _) => RebuildVideoDisplayList();
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
}
