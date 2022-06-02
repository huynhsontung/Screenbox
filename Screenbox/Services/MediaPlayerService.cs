#nullable enable

using System;
using Windows.Foundation;
using Windows.Media.Devices;
using Windows.Storage.AccessCache;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal class MediaPlayerService : IMediaPlayerService, IDisposable
    {
        public event EventHandler? PlayerInitialized;
        public event EventHandler<MediaPlayerMediaChangedEventArgs>? MediaChanged;
        public event EventHandler<MediaPlayerLengthChangedEventArgs>? LengthChanged;
        public event EventHandler<MediaPlayerTimeChangedEventArgs>? TimeChanged;
        public event EventHandler<MediaPlayerSeekableChangedEventArgs>? SeekableChanged;
        public event EventHandler<MediaPlayerBufferingEventArgs>? Buffering;
        public event EventHandler<MediaPlayerChapterChangedEventArgs>? ChapterChanged;
        public event EventHandler<MediaPlayerTitleChangedEventArgs>? TitleChanged;
        public event EventHandler<MediaPlayerVolumeChangedEventArgs>? VolumeChanged;
        public event EventHandler? Muted;
        public event EventHandler? Unmuted;
        public event EventHandler? EndReached;
        public event EventHandler? Playing;
        public event EventHandler? Paused;
        public event EventHandler? Stopped;
        public event EventHandler? EncounteredError;
        public event EventHandler? Opening;

        public MediaPlayer? VlcPlayer { get; private set; }

        public LibVLC? LibVlc { get; private set; }

        public MediaHandle? CurrentMedia { get; private set; }

        public int Volume
        {
            get => VlcPlayer?.Volume ?? 100;
            set
            {
                if (VlcPlayer == null) return;
                // VLC is fine with taking volume >100. It will amplify the audio signal.
                value = Math.Clamp(value, 0, 100);
                VlcPlayer.Volume = value;
            }
        }

        public double? NumericAspectRatio
        {
            get
            {
                uint px = 0, py = 0;
                return (VlcPlayer?.Size(0, ref px, ref py) ?? false) && py != 0 ? (double)px / py : null;
            }
        }

        public Size Dimension
        {
            get
            {
                uint px = 0, py = 0;
                return VlcPlayer?.Size(0, ref px, ref py) ?? false ? new Size(px, py) : Size.Empty;
            }
        }

        public float Rate
        {
            get => VlcPlayer?.Rate ?? default;
            set => VlcPlayer?.SetRate(value);
        }

        public string? CropGeometry
        {
            get => VlcPlayer?.CropGeometry;
            set
            {
                if (VlcPlayer != null)
                    VlcPlayer.CropGeometry = value;
            }
        }

        public long FrameDuration => VlcPlayer?.Fps != 0 ? (long)Math.Ceiling(1000.0 / VlcPlayer?.Fps ?? 1) : 0;

        public MediaPlayerService()
        {
            // Notify VLC to auto detect new audio device on device changed
            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
        }

        public void InitVlcPlayer(string[] swapChainOptions)
        {
            LibVlc?.Dispose();
            LibVlc = InitializeLibVlc(swapChainOptions);
            VlcPlayer?.Dispose();
            VlcPlayer = new MediaPlayer(LibVlc);
            RegisterEventForwarding(VlcPlayer);
            PlayerInitialized?.Invoke(this, EventArgs.Empty);

            // Clear FA periodically because of 1000 items limit
            StorageApplicationPermissions.FutureAccessList.Clear();
        }

        public void Replay()
        {
            Stop();
            Play();
        }

        public void Play(MediaHandle media)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            CurrentMedia = media;
            VlcPlayer.Play(media.Media);
        }

        public void Play() => VlcPlayer?.Play();

        public void Pause() => VlcPlayer?.Pause();

        public void SetAudioOutputDevice(string? deviceId = null)
        {
            if (VlcPlayer == null) return;
            deviceId ??= VlcPlayer.OutputDevice;
            if (deviceId == null) return;
            VlcPlayer.SetOutputDevice(deviceId);
        }

        public void NextFrame() => VlcPlayer?.NextFrame();

        public void Stop() => VlcPlayer?.Stop();

        public long SetTime(double time)
        {
            if (VlcPlayer == null || VlcPlayer.Length < 0) return -1;
            time = Math.Clamp(time, 0, VlcPlayer.Length);
            if (VlcPlayer.State == VLCState.Ended)
            {
                Replay();
            }

            return VlcPlayer.Time = (long)time;
        }

        public long Seek(double amount)
        {
            if (VlcPlayer == null) return -1;
            return SetTime(VlcPlayer.Time + amount);
        }

        public void AddSubtitle(string mrl)
        {
            VlcPlayer?.AddSlave(MediaSlaveType.Subtitle, mrl, true);
        }

        public void Dispose()
        {
            LibVlc?.Dispose();
            VlcPlayer?.Dispose();
        }

        private LibVLC InitializeLibVlc(string[] swapChainOptions)
        {
            string[] options = new string[swapChainOptions.Length + 1];
            options[0] = "--no-osd";
            swapChainOptions.CopyTo(options, 1);
            LibVLC libVlc = new(true, options);
            LogService.RegisterLibVlcLogging(libVlc);
            return libVlc;
        }

        private void RegisterEventForwarding(MediaPlayer player)
        {
            player.MediaChanged += (s, e) => MediaChanged?.Invoke(s, e);
            player.LengthChanged += (s, e) => LengthChanged?.Invoke(s, e);
            player.TimeChanged += (s, e) => TimeChanged?.Invoke(s, e);
            player.SeekableChanged += (s, e) => SeekableChanged?.Invoke(s, e);
            player.ChapterChanged += (s, e) => ChapterChanged?.Invoke(s, e);
            player.TitleChanged += (s, e) => TitleChanged?.Invoke(s, e);
            player.Buffering += (s, e) => Buffering?.Invoke(s, e);
            player.VolumeChanged += (s, e) => VolumeChanged?.Invoke(s, e);
            player.Muted += (s, e) => Muted?.Invoke(s, e);
            player.Unmuted += (s, e) => Unmuted?.Invoke(s, e);
            player.EndReached += (s, e) => EndReached?.Invoke(s, e);
            player.Playing += (s, e) => Playing?.Invoke(s, e);
            player.Paused += (s, e) => Paused?.Invoke(s, e);
            player.Stopped += (s, e) => Stopped?.Invoke(s, e);
            player.EncounteredError += (s, e) => EncounteredError?.Invoke(s, e);
            player.Opening += (s, e) => Opening?.Invoke(s, e);
        }

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            if (args.Role == AudioDeviceRole.Default)
            {
                SetAudioOutputDevice();
            }
        }
    }
}
