#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Playback;
using AudioTrack = Screenbox.Core.Playback.AudioTrack;
using SubtitleTrack = Screenbox.Core.Playback.SubtitleTrack;

namespace Screenbox.ViewModels
{
    internal partial class AudioTrackSubtitleViewModel : ObservableRecipient, IRecipient<MediaPlayerChangedMessage>
    {
        public int SubtitleTrackIndex
        {
            get => _subtitleTrackIndex;
            set
            {
                if (!SetProperty(ref _subtitleTrackIndex, value)) return;
                if (ItemSubtitleTrackList != null && value >= 0 && value < SubtitleTracks.Count)
                    ItemSubtitleTrackList.SelectedIndex = value - 1;
            }
        }

        public int AudioTrackIndex
        {
            get => _audioTrackIndex;
            set
            {
                if (!SetProperty(ref _audioTrackIndex, value)) return;
                if (ItemAudioTrackList != null && value >= 0 && value < AudioTracks.Count)
                    ItemAudioTrackList.SelectedIndex = value;
            }
        }

        public ObservableCollection<string> SubtitleTracks { get; }

        public ObservableCollection<string> AudioTracks { get; }

        private PlaybackSubtitleTrackList? ItemSubtitleTrackList => _mediaPlayer?.PlaybackItem?.SubtitleTracks;

        private PlaybackAudioTrackList? ItemAudioTrackList => _mediaPlayer?.PlaybackItem?.AudioTracks;

        private readonly IFilesService _filesService;
        private IMediaPlayer? _mediaPlayer;
        private int _subtitleTrackIndex;
        private int _audioTrackIndex;

        public AudioTrackSubtitleViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            SubtitleTracks = new ObservableCollection<string>();
            AudioTracks = new ObservableCollection<string>();

            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
        }

        [ICommand]
        private async Task AddSubtitle()
        {
            if (_mediaPlayer?.Source == null) return;
            try
            {
                StorageFile? file = await _filesService.PickFileAsync(".srt", ".ass");
                if (file == null) return;

                _mediaPlayer?.AddSubtitle(file);
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(Resources.FailedToLoadSubtitleNotificationTitle, e.ToString()));
            }
        }

        public void OnAudioCaptionFlyoutOpening()
        {
            UpdateSubtitleTrackList();
            UpdateAudioTrackList();
            SubtitleTrackIndex = ItemSubtitleTrackList?.SelectedIndex + 1 ?? -1;
            AudioTrackIndex = ItemAudioTrackList?.SelectedIndex ?? -1;
        }

        private void UpdateAudioTrackList()
        {
            if (ItemAudioTrackList == null) return;
            AudioTracks.Clear();
            if (ItemAudioTrackList.Count <= 0) return;

            for (int index = 0; index < ItemAudioTrackList.Count; index++)
            {
                AudioTrack audioTrack = ItemAudioTrackList[index];
                AudioTracks.Add(audioTrack.Label ?? $"Track {index + 1}");
            }
        }

        private void UpdateSubtitleTrackList()
        {
            if (ItemSubtitleTrackList == null) return;
            SubtitleTracks.Clear();
            if (ItemSubtitleTrackList.Count <= 0) return;

            SubtitleTracks.Add(Resources.Disable);
            for (int index = 0; index < ItemSubtitleTrackList.Count; index++)
            {
                SubtitleTrack subtitleTrack = ItemSubtitleTrackList[index];
                SubtitleTracks.Add(subtitleTrack.Label ?? $"Track {index + 1}");
            }
        }
    }
}
