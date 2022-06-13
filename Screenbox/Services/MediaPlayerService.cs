#nullable enable

using System;
using System.Collections.Generic;
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
        public event EventHandler<EventArgs>? PlayerInitialized;
        public event EventHandler<MediaParsedChangedEventArgs>? MediaParsed;
        public event EventHandler<MediaPlayerMediaChangedEventArgs>? MediaChanged;
        public event EventHandler<MediaPlayerLengthChangedEventArgs>? LengthChanged;
        public event EventHandler<MediaPlayerTimeChangedEventArgs>? TimeChanged;
        public event EventHandler<MediaPlayerSeekableChangedEventArgs>? SeekableChanged;
        public event EventHandler<MediaPlayerBufferingEventArgs>? Buffering;
        public event EventHandler<MediaPlayerChapterChangedEventArgs>? ChapterChanged;
        public event EventHandler<MediaPlayerTitleChangedEventArgs>? TitleChanged;
        public event EventHandler<MediaPlayerVolumeChangedEventArgs>? VolumeChanged;
        public event EventHandler<PlayerStateChangedEventArgs>? StateChanged; 
        public event EventHandler<EventArgs>? Muted;
        public event EventHandler<EventArgs>? Unmuted;
        public event EventHandler<EventArgs>? EndReached;
        public event EventHandler<EventArgs>? Playing;
        public event EventHandler<EventArgs>? Paused;
        public event EventHandler<EventArgs>? Stopped;
        public event EventHandler<EventArgs>? EncounteredError;
        public event EventHandler<EventArgs>? Opening;

        public MediaPlayer? VlcPlayer { get; private set; }

        public LibVLC? LibVlc { get; private set; }

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

        public VLCState State
        {
            get => _state;
            private set
            {
                VLCState oldValue = _state;
                if (oldValue != value)
                {
                    _state = value;
                    StateChanged?.Invoke(this, new PlayerStateChangedEventArgs(value, oldValue));
                }
            }
        }

        private VLCState _state;

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
            List<string> options = new(swapChainOptions.Length + 4)
            {
#if DEBUG
                "--verbose=3",
#else
                "--verbose=0",
#endif
                "--aout=winstore",
                //"--sout-chromecast-conversion-quality=0",
                "--no-osd"
            };
            options.AddRange(swapChainOptions);
            LibVLC libVlc = new(true, options.ToArray());
            LogService.RegisterLibVlcLogging(libVlc);
            return libVlc;
        }

        private void UpdateStateAndFireEvent(EventHandler<EventArgs>? handler, object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            State = VlcPlayer.State;
            handler?.Invoke(sender, e);
        }

        private void RegisterEventForwarding(MediaPlayer player)
        {
            player.MediaChanged += (s, e) =>
            {
                MediaChanged?.Invoke(s, e);
                e.Media.ParsedChanged += MediaParsed;
            };
            player.LengthChanged += LengthChanged;
            player.TimeChanged += TimeChanged;
            player.SeekableChanged += SeekableChanged;
            player.ChapterChanged += ChapterChanged;
            player.TitleChanged += TitleChanged;
            player.Buffering += Buffering;
            player.VolumeChanged += VolumeChanged;
            player.Muted += Muted;
            player.Unmuted += Unmuted;
            player.EndReached += (s, e) => UpdateStateAndFireEvent(EndReached, s, e);
            player.Playing += (s, e) => UpdateStateAndFireEvent(Playing, s, e);
            player.Paused += (s, e) => UpdateStateAndFireEvent(Paused, s, e);
            player.Stopped += (s, e) => UpdateStateAndFireEvent(Stopped, s, e);
            player.EncounteredError += (s, e) => UpdateStateAndFireEvent(EncounteredError, s, e);
            player.Opening += (s, e) => UpdateStateAndFireEvent(Opening, s, e);
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
