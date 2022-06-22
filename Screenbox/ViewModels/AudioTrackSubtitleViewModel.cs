#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Storage;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Playback;

namespace Screenbox.ViewModels
{
    internal partial class AudioTrackSubtitleViewModel : ObservableRecipient
    {
        private MediaPlayer? VlcPlayer => _mediaPlayer?.VlcPlayer;

        public int SpuIndex
        {
            get => _spuIndex;
            set
            {
                if (!SetProperty(ref _spuIndex, value)) return;
                var spuDesc = SpuDescriptions;
                if (value >= 0 && value < spuDesc.Length)
                    VlcPlayer?.SetSpu(spuDesc[value].Id);
            }
        }

        public int AudioTrackIndex
        {
            get => _audioTrackIndex;
            set
            {
                if (!SetProperty(ref _audioTrackIndex, value)) return;
                var audioDesc = AudioTrackDescriptions;
                if (value >= 0 && value < audioDesc.Length)
                    VlcPlayer?.SetSpu(audioDesc[value].Id);
            }
        }

        [ObservableProperty]
        private TrackDescription[] _spuDescriptions;

        [ObservableProperty]
        private TrackDescription[] _audioTrackDescriptions;

        private readonly IFilesService _filesService;
        private VlcMediaPlayer? _mediaPlayer;
        private int _spuIndex;
        private int _audioTrackIndex;

        public AudioTrackSubtitleViewModel(IFilesService filesService, LibVlcService libVlcService)
        {
            libVlcService.Initialized += LibVlcService_Initialized;
            _filesService = filesService;
            _spuDescriptions = Array.Empty<TrackDescription>();
            _audioTrackDescriptions = Array.Empty<TrackDescription>();
        }

        private void LibVlcService_Initialized(LibVlcService sender, Core.MediaPlayerInitializedEventArgs args)
        {
            _mediaPlayer = (VlcMediaPlayer)args.MediaPlayer;
        }

        [ICommand]
        private async Task AddSubtitle()
        {
            if (VlcPlayer == null || !VlcPlayer.WillPlay) return;
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
            UpdateSpuOptions();
            UpdateAudioTrackOptions();
        }

        private void UpdateSpuOptions()
        {
            if (VlcPlayer == null) return;
            int spu = VlcPlayer.Spu;
            SpuDescriptions = VlcPlayer.SpuDescription;
            SpuIndex = GetIndexFromTrackId(spu, SpuDescriptions);
        }

        private void UpdateAudioTrackOptions()
        {
            if (VlcPlayer == null) return;
            int audioTrack = VlcPlayer.AudioTrack;
            AudioTrackDescriptions = VlcPlayer.AudioTrackDescription;
            AudioTrackIndex = GetIndexFromTrackId(audioTrack, AudioTrackDescriptions);
        }

        private static int GetIndexFromTrackId(int id, TrackDescription[] tracks)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].Id == id) return i;
            }

            return -1;
        }
    }
}
