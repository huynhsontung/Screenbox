#nullable enable

using System;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Devices;
using Windows.Storage.AccessCache;
using Windows.System;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;

namespace Screenbox.Services
{
    public class MediaPlayerService : IMediaPlayerService, IDisposable
    {
        public event EventHandler? VlcPlayerChanged;

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

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly SystemMediaTransportControls _transportControl;

        public MediaPlayerService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _transportControl = SystemMediaTransportControls.GetForCurrentView();

            _transportControl.ButtonPressed += TransportControl_ButtonPressed;
            _transportControl.IsEnabled = true;
            _transportControl.IsPlayEnabled = true;
            _transportControl.IsPauseEnabled = true;
            _transportControl.IsStopEnabled = true;
            _transportControl.PlaybackStatus = MediaPlaybackStatus.Closed;

            SystemMediaTransportControlsDisplayUpdater displayUpdater = _transportControl.DisplayUpdater;
            displayUpdater.ClearAll();
            displayUpdater.AppMediaId = "Modern VLC";

            // Notify VLC to auto detect new audio device on device changed
            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
        }

        public void InitVlcPlayer(string[] swapChainOptions)
        {
            LibVlc?.Dispose();
            LibVlc = InitializeLibVlc(swapChainOptions);
            VlcPlayer?.Dispose();
            VlcPlayer = new MediaPlayer(LibVlc);
            VlcPlayerChanged?.Invoke(this, EventArgs.Empty);
            RegisterPlaybackEvents();

            // Clear FA periodically because of 1000 items limit
            StorageApplicationPermissions.FutureAccessList.Clear();
        }

        public void Replay()
        {
            Stop();
            Play();
        }

        public void Play(Media media)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            VlcPlayer.Play(media);
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
            if (VlcPlayer == null) return -1;
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
            VlcPlayerChanged = null;
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

        private void RegisterPlaybackEvents()
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            
            VlcPlayer.Paused += (_, _) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Paused);
            VlcPlayer.Stopped += (_, _) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Stopped);
            VlcPlayer.Playing += (_, _) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Playing);
            VlcPlayer.EncounteredError += (_, _) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Closed);
            VlcPlayer.Opening += (_, _) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Changing);
        }

        private void TransportControl_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    Play();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    Stop();
                    break;
                //case SystemMediaTransportControlsButton.Previous:
                //    Locator.PlaybackService.Previous();
                //    break;
                //case SystemMediaTransportControlsButton.Next:
                //    Locator.PlaybackService.Next();
                //    break;
                case SystemMediaTransportControlsButton.FastForward:
                    Seek(30000);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    Seek(-30000);
                    break;
            }
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
