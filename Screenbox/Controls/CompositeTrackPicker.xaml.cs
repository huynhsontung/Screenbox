using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
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

        private bool _updatingSubtitleIndex;

        public CompositeTrackPicker()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<CompositeTrackPickerViewModel>();

            ViewModel.SubtitleTracks.CollectionChanged += (_, _) => RebuildSubtitleDisplayList();
            ViewModel.AudioTracks.CollectionChanged += (_, _) => RebuildAudioDisplayList();
            ViewModel.VideoTracks.CollectionChanged += (_, _) => RebuildVideoDisplayList();
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>Formats a track's display name, falling back to "Track N" when the label is empty.</summary>
        private static string GetTrackDisplayName(string trackLabel, int oneBasedIndex) =>
            !string.IsNullOrEmpty(trackLabel)
                ? trackLabel
                : Screenbox.Strings.Resources.TrackIndex(oneBasedIndex);

        private void RebuildSubtitleDisplayList()
        {
            SubtitleDisplayList.Clear();
            // Index 0 = "Disable" in the display list (maps to VM SubtitleTrackIndex = -1)
            SubtitleDisplayList.Add(Screenbox.Strings.Resources.Disable);
            for (int i = 0; i < ViewModel.SubtitleTracks.Count; i++)
            {
                SubtitleDisplayList.Add(GetTrackDisplayName(ViewModel.SubtitleTracks[i], i + 1));
            }

            // Re-sync the display index after the list is rebuilt
            SyncSubtitleDisplayIndex();
        }

        private void RebuildAudioDisplayList()
        {
            AudioDisplayList.Clear();
            for (int i = 0; i < ViewModel.AudioTracks.Count; i++)
            {
                AudioDisplayList.Add(GetTrackDisplayName(ViewModel.AudioTracks[i], i + 1));
            }
        }

        private void RebuildVideoDisplayList()
        {
            VideoDisplayList.Clear();
            for (int i = 0; i < ViewModel.VideoTracks.Count; i++)
            {
                VideoDisplayList.Add(GetTrackDisplayName(ViewModel.VideoTracks[i], i + 1));
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompositeTrackPickerViewModel.SubtitleTrackIndex))
            {
                SyncSubtitleDisplayIndex();
            }
        }

        /// <summary>
        /// Syncs the subtitle ListView's selected index to reflect the VM's SubtitleTrackIndex.
        /// VM index -1 (disabled) maps to display index 0 ("Disable"); VM index N maps to display index N+1.
        /// </summary>
        private void SyncSubtitleDisplayIndex()
        {
            if (_updatingSubtitleIndex) return;
            _updatingSubtitleIndex = true;
            SubtitleTrackListView.SelectedIndex = ViewModel.SubtitleTrackIndex + 1;
            _updatingSubtitleIndex = false;
        }

        private void SubtitleTrackListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingSubtitleIndex) return;
            var listView = (ListView)sender;
            _updatingSubtitleIndex = true;
            // Display index 0 = "Disable" -> VM index -1; display index N -> VM index N-1
            ViewModel.SubtitleTrackIndex = listView.SelectedIndex - 1;
            _updatingSubtitleIndex = false;
        }
    }
}
