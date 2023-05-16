#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using AudioTrack = Screenbox.Core.Playback.AudioTrack;
using SubtitleTrack = Screenbox.Core.Playback.SubtitleTrack;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class AudioTrackSubtitleViewModel : ObservableRecipient, IRecipient<MediaPlayerChangedMessage>
    {
        public ObservableCollection<string> SubtitleTracks { get; }

        public ObservableCollection<string> AudioTracks { get; }

        private PlaybackSubtitleTrackList? ItemSubtitleTrackList => _mediaPlayer?.PlaybackItem?.SubtitleTracks;

        private PlaybackAudioTrackList? ItemAudioTrackList => _mediaPlayer?.PlaybackItem?.AudioTracks;

        [ObservableProperty] private int _subtitleTrackIndex;
        [ObservableProperty] private int _audioTrackIndex;
        private readonly IFilesService _filesService;
        private readonly IResourceService _resourceService;
        private IMediaPlayer? _mediaPlayer;

        public AudioTrackSubtitleViewModel(IFilesService filesService, IResourceService resourceService)
        {
            _filesService = filesService;
            _resourceService = resourceService;
            SubtitleTracks = new ObservableCollection<string>();
            AudioTracks = new ObservableCollection<string>();
            _mediaPlayer = Messenger.Send(new MediaPlayerRequestMessage()).Response;

            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
        }

        partial void OnSubtitleTrackIndexChanged(int value)
        {
            if (ItemSubtitleTrackList != null && value >= 0 && value < SubtitleTracks.Count)
                ItemSubtitleTrackList.SelectedIndex = value - 1;
        }

        partial void OnAudioTrackIndexChanged(int value)
        {
            if (ItemAudioTrackList != null && value >= 0 && value < AudioTracks.Count)
                ItemAudioTrackList.SelectedIndex = value;
        }

        [RelayCommand]
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
                Messenger.Send(new ErrorMessage(
                    _resourceService.GetString(ResourceName.FailedToLoadSubtitleNotificationTitle), e.ToString()));
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
                string defaultTrackLabel = _resourceService.GetString(ResourceName.TrackIndex, index + 1);
                AudioTracks.Add(audioTrack.Label ?? defaultTrackLabel);
            }
        }

        private void UpdateSubtitleTrackList()
        {
            if (ItemSubtitleTrackList == null) return;
            SubtitleTracks.Clear();
            if (ItemSubtitleTrackList.Count <= 0) return;

            SubtitleTracks.Add(_resourceService.GetString(ResourceName.Disable));
            for (int index = 0; index < ItemSubtitleTrackList.Count; index++)
            {
                SubtitleTrack subtitleTrack = ItemSubtitleTrackList[index];
                string defaultTrackLabel = _resourceService.GetString(ResourceName.TrackIndex, index + 1);
                SubtitleTracks.Add(subtitleTrack.Label ?? defaultTrackLabel);
            }
        }
    }
}
