#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class AudioTrackSubtitleViewModel : ObservableObject
    {
        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

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

        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly INotificationService _notificationService;
        private readonly IFilesService _filesService;
        private int _spuIndex;
        private int _audioTrackIndex;

        public AudioTrackSubtitleViewModel(
            IMediaPlayerService mediaPlayerService,
            INotificationService notificationService,
            IFilesService filesService)
        {
            _mediaPlayerService = mediaPlayerService;
            _notificationService = notificationService;
            _filesService = filesService;
            _spuDescriptions = Array.Empty<TrackDescription>();
            _audioTrackDescriptions = Array.Empty<TrackDescription>();
        }

        [ICommand]
        private async Task AddSubtitle()
        {
            if (VlcPlayer == null || !VlcPlayer.WillPlay) return;
            try
            {
                StorageFile? file = await _filesService.PickFileAsync(".srt", ".ass");
                if (file == null) return;

                string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "subtitle");
                _mediaPlayerService.AddSubtitle(mrl);
            }
            catch (Exception e)
            {
                _notificationService.RaiseError("Failed to load subtitle", e.ToString());
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
