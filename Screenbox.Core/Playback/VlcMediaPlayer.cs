#nullable enable

using LibVLCSharp.Shared;
using Screenbox.Core.Events;
using Screenbox.Core.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.AccessCache;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Screenbox.Core.Playback
{
    public sealed class VlcMediaPlayer : IMediaPlayer
    {
        public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaEnded;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaFailed;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaOpened;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? IsMutedChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? VolumeChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<PlaybackItem?>>? PlaybackItemChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingProgressChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingStarted;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingEnded;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<TimeSpan>>? NaturalDurationChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? NaturalVideoSizeChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? CanSeekChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<TimeSpan>>? PositionChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<ChapterCue?>>? ChapterChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<MediaPlaybackState>>? PlaybackStateChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<double>>? PlaybackRateChanged;

        public ChapterCue? Chapter
        {
            get => _chapter;
            set
            {
                if (value == _chapter) return;
                ChapterCue? oldValue = _chapter;
                _chapter = value;
                ChapterChanged?.Invoke(this, new ValueChangedEventArgs<ChapterCue?>(value, oldValue));
            }
        }

        public TimeSpan NaturalDuration
        {
            get => _naturalDuration;
            private set
            {
                // Length can fluctuate during playback. Check for tolerance here.
                if (Math.Abs((_naturalDuration - value).TotalMilliseconds) <= 50) return;
                TimeSpan oldValue = _naturalDuration;
                _naturalDuration = value;
                NaturalDurationChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(value, oldValue));
            }
        }

        public TimeSpan Position
        {
            get => _position;
            set
            {
                if (VlcPlayer.Length < 0) return;
                if (value < TimeSpan.Zero) value = TimeSpan.Zero;
                if (value > NaturalDuration) value = NaturalDuration;
                if (VlcPlayer.State == VLCState.Ended)
                {
                    if (value == NaturalDuration)
                    {
                        _position = value;
                        return;
                    }

                    Replay();
                }

                TimeSpan oldValue = _position;
                _position = value;
                long ms = (long)value.TotalMilliseconds;
                VlcPlayer.Time = ms;
                // Position changed will not fire if the player is paused
                // TODO: Check for more problematic states
                if (PlaybackState is MediaPlaybackState.Paused)
                {
                    PositionChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(value, oldValue));
                }
            }
        }

        public bool IsMuted
        {
            get => VlcPlayer.Mute;
            set
            {
                if (VlcPlayer.Mute != value)
                {
                    VlcPlayer.Mute = value;
                }
            }
        }

        public double Volume
        {
            get => VlcPlayer.Volume / 100d;
            set
            {
                int iVal = (int)(value * 100);
                if (VlcPlayer.Volume != iVal && VlcPlayer.Volume >= 0)
                {
                    VlcPlayer.Volume = iVal;
                }
            }
        }

        public double PlaybackRate
        {
            get => VlcPlayer.Rate;
            set
            {
                if (Math.Abs(VlcPlayer.Rate - value) > 0.0001)
                {
                    double oldValue = VlcPlayer.Rate;
                    VlcPlayer.SetRate((float)value);
                    PlaybackRateChanged?.Invoke(this, new ValueChangedEventArgs<double>(value, oldValue));
                }
            }
        }

        public Rect NormalizedSourceRect
        {
            get => _normalizedSourceRect;
            set
            {
                if (value == _defaultSourceRect)
                {
                    _normalizedSourceRect = _defaultSourceRect;
                    VlcPlayer.CropGeometry = null;
                }
                else
                {
                    _normalizedSourceRect = value;

                    /*
                    This is how CropGeometry is parsed. Note that it only takes integer inputs.
                    https://code.videolan.org/videolan/vlc/-/blob/56222b9290dd9bf08e02b10b1e9ee13d68931fc2/src/video_output/vout_intf.c#L452-464

                    if (sscanf(newval.psz_string, "%u:%u", &num, &den) == 2) {
                       vout_ControlChangeCropRatio(vout, num, den);
                    } else if (sscanf(newval.psz_string, "%ux%u+%u+%u", &width, &height, &x, &y) == 4) {
                       vout_ControlChangeCropWindow(vout, x, y, width, height);
                    } else if (sscanf(newval.psz_string, "%u+%u+%u+%u", &left, &top, &right, &bottom) == 4) {
                       vout_ControlChangeCropBorder(vout, left, top, right, bottom);
                    } else if (*newval.psz_string == '\0') {
                       vout_ControlChangeCropRatio(vout, 0, 0);
                    } else {
                       msg_Err(object, "Unknown crop format (%s)", newval.psz_string);
                    }
                    */

                    // double rightOffset = value.Right * NaturalVideoWidth;
                    // double bottomOffset = value.Bottom * NaturalVideoHeight;
                    // double leftOffset = value.Left * NaturalVideoWidth;
                    // double topOffset = value.Top * NaturalVideoHeight;
                    // VlcPlayer.CropGeometry = $"{rightOffset:F0}x{bottomOffset:F0}+{leftOffset:F0}+{topOffset:F0}";

                    // Use crop ratio to avoid conflict with subtitle rendering
                    double newWidth = value.Width * NaturalVideoWidth;
                    double newHeight = value.Height * NaturalVideoHeight;
                    VlcPlayer.CropGeometry = $"{newWidth:F0}:{newHeight:F0}";
                }
            }
        }

        public DeviceInformation? AudioDevice
        {
            get => null;    // TODO: Implement AudioDevice getter
            set
            {
                string? deviceId = value?.Id;
                deviceId ??= VlcPlayer.OutputDevice;
                if (deviceId == null) return;
                VlcPlayer.SetOutputDevice(deviceId);
            }
        }

        public MediaPlaybackState PlaybackState
        {
            get => _playbackState;
            private set
            {
                if (value == _playbackState) return;
                MediaPlaybackState oldValue = _playbackState;
                _playbackState = value;
                PlaybackStateChanged?.Invoke(this, new ValueChangedEventArgs<MediaPlaybackState>(value, oldValue));
            }
        }

        public PlaybackItem? PlaybackItem
        {
            get => _playbackItem;
            set
            {
                if (_playbackItem == value) return;
                PlaybackItem? oldValue = _playbackItem;
                if (value == null)
                {
                    VlcPlayer.Stop();
                    if (_playbackItem != null) RemoveItemHandlers(_playbackItem);
                    _playbackItem = null;
                }
                else
                {
                    _playbackItem = value;
                    RegisterItemHandlers(_playbackItem);
                    _readyToPlay = true;
                    _updateMediaProperties = true;
                }

                PlaybackItemChanged?.Invoke(this, new ValueChangedEventArgs<PlaybackItem?>(value, oldValue));
            }
        }

        public bool CanSeek { get; private set; }

        public bool IsLoopingEnabled { get; set; }

        public double BufferingProgress { get; private set; }

        public uint NaturalVideoHeight { get; private set; }

        public uint NaturalVideoWidth { get; private set; }

        public bool CanPause => VlcPlayer.CanPause;

        internal MediaPlayer VlcPlayer { get; }

        internal LibVLC LibVlc { get; }

        private readonly Rect _defaultSourceRect;
        private ChapterCue? _chapter;
        private Rect _normalizedSourceRect;
        private bool _readyToPlay;
        private bool _updateMediaProperties;
        private TimeSpan _naturalDuration;
        private TimeSpan _position;
        private MediaPlaybackState _playbackState;
        private PlaybackItem? _playbackItem;
        private AudioGraph? _audioGraph;
        private AudioFrameInputNode? _inputNode;
        private AudioDeviceOutputNode? _outputNode;
        private CircularBuffer<short>? _audioBuffer;

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        public VlcMediaPlayer(LibVLC libVlc)
        {
            LibVlc = libVlc;
            VlcPlayer = new MediaPlayer(libVlc);
            _defaultSourceRect = new Rect(0, 0, 1, 1);
            _normalizedSourceRect = _defaultSourceRect;

            VlcPlayer.TimeChanged += VlcPlayer_TimeChanged;
            VlcPlayer.Muted += (s, e) => IsMutedChanged?.Invoke(this, EventArgs.Empty);
            VlcPlayer.Unmuted += (s, e) => IsMutedChanged?.Invoke(this, EventArgs.Empty);
            VlcPlayer.VolumeChanged += (s, e) => VolumeChanged?.Invoke(this, EventArgs.Empty);
            VlcPlayer.Paused += (s, e) => PlaybackState = MediaPlaybackState.Paused;
            VlcPlayer.Stopped += (s, e) => PlaybackState = MediaPlaybackState.None;
            VlcPlayer.EncounteredError += VlcPlayer_EncounteredError;
            VlcPlayer.Playing += VlcPlayer_Playing;
            VlcPlayer.ChapterChanged += VlcPlayer_ChapterChanged;
            VlcPlayer.LengthChanged += VlcPlayer_LengthChanged;
            VlcPlayer.EndReached += VlcPlayer_EndReached;
            VlcPlayer.Buffering += VlcPlayer_Buffering;
            VlcPlayer.Opening += VlcPlayer_Opening;
            VlcPlayer.ESAdded += VlcPlayer_ESAdded;
            VlcPlayer.ESSelected += VlcPlayer_ESSelected;
            VlcPlayer.SeekableChanged += VlcPlayer_SeekableChanged;

            // Notify VLC to auto detect new audio device on device changed
            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
        }

        public async Task InitAudioGraphAsync()
        {
            AudioGraphSettings settings = new(Windows.Media.Render.AudioRenderCategory.Media);
            // settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency;
            // settings.MaxPlaybackSpeedFactor = 3;
            // settings.EncodingProperties = AudioEncodingProperties.CreatePcm(48000, 2, 16);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);
            if (result.Status != AudioGraphCreationStatus.Success) return;
            AudioGraph audioGraph = _audioGraph = result.Graph;
            var outputNodeResult = await audioGraph.CreateDeviceOutputNodeAsync();
            if (outputNodeResult.Status != AudioDeviceNodeCreationStatus.Success) return;
            var outputNode = _outputNode = outputNodeResult.DeviceOutputNode;

            // AudioEncodingProperties inputEncoding = AudioEncodingProperties.CreatePcm(
            //     audioGraph.EncodingProperties.SampleRate, audioGraph.EncodingProperties.ChannelCount, 16);
            // var inputNode = _inputNode = audioGraph.CreateFrameInputNode(inputEncoding);
            // inputNode.AddOutgoingConnection(outputNode);
            // inputNode.QuantumStarted += InputNodeOnQuantumStarted;

            // VLC callbacks
            VlcPlayer.SetAudioFormatCallback(SetupCb, CleanupCb);
            // VlcPlayer.SetAudioFormat("S16N", inputEncoding.SampleRate, inputEncoding.ChannelCount);
            VlcPlayer.SetAudioCallbacks(PlayCb, PauseCb, ResumeCb, FlushCb, DrainCb);
            VlcPlayer.SetVolumeCallback(VolumeCb);

            audioGraph.Start();
        }

        private void CleanupCb(IntPtr opaque)
        {
            if (_inputNode == null) return;
            _audioGraph?.Stop();
            _audioGraph?.ResetAllNodes();

            _inputNode.RemoveOutgoingConnection(_outputNode);
            _inputNode.Dispose();
        }

        private int SetupCb(ref IntPtr opaque, ref IntPtr format, ref uint rate, ref uint channels)
        {
            // Format is always "S16N"
            if (_audioGraph == null) return 0;

            // Create an audio buffer for 1s of audio
            _audioBuffer = new CircularBuffer<short>((int)(rate * channels));

            // Create input node that has 16-bit integer PCM encoding to match VLC
            var inputEncoding = AudioEncodingProperties.CreatePcm(rate, channels, 16);
            var inputNode = _inputNode = _audioGraph.CreateFrameInputNode(inputEncoding);
            inputNode.AddOutgoingConnection(_outputNode);
            inputNode.QuantumStarted += InputNodeOnQuantumStarted;
            _audioGraph.Start();

            // rate = _inputNode.EncodingProperties.SampleRate;
            // channels = _inputNode.EncodingProperties.ChannelCount;
            // var span = new ReadOnlySpan<byte>((void*)format, 4);
            // _format = System.Text.Encoding.ASCII.GetString(span.ToArray());
            return 0;
        }

        private void VolumeCb(IntPtr data, float volume, bool mute)
        {
            if (_inputNode == null) return;
            _inputNode.OutgoingGain = volume;
        }

        private void InputNodeOnQuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            AudioFrame frame;
            lock (_audioBuffer)
            {
                if (_audioBuffer.Size < args.RequiredSamples * sender.EncodingProperties.ChannelCount) return;
                frame = GenerateAudioFrame((uint)args.RequiredSamples, sender.EncodingProperties.ChannelCount, sender.EncodingProperties.SampleRate);
            }

            sender.AddFrame(frame);

            // if (_frameBuffer.TryDequeue(out AudioFrame frame))
            // {
            //     sender.AddFrame(frame);
            // }

            // if (_inputNode == null) return;
            // uint numSamplesNeeded = (uint)args.RequiredSamples;
            //
            // if (numSamplesNeeded != 0)
            // {
            //     AudioFrame audioData = GenerateAudioData(numSamplesNeeded, _inputNode.EncodingProperties.ChannelCount);
            //     _inputNode.AddFrame(audioData);
            // }
        }

        private double audioWaveTheta = 0;

        private unsafe AudioFrame GenerateAudioData(uint samples, uint numChannels)
        {
            // Buffer size is (number of samples) * (size of each sample)
            // We choose to generate single channel (mono) audio. For multi-channel, multiply by number of channels
            uint bufferSize = samples * numChannels * sizeof(float);
            AudioFrame frame = new(bufferSize);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                // Cast to float since the data we are generating is float
                dataInFloat = (float*)dataInBytes;

                float freq = 1000; // choosing to generate frequency of 1kHz
                float amplitude = 0.3f;
                int sampleRate = (int)_audioGraph.EncodingProperties.SampleRate;
                double sampleIncrement = (freq * (Math.PI * 2)) / sampleRate;

                // Generate a 1kHz sine wave and populate the values in the memory buffer
                for (uint i = 0; i < samples; i++)
                {
                    double sinValue = amplitude * Math.Sin(audioWaveTheta);
                    dataInFloat[i] = (float)sinValue;
                    dataInFloat[i + samples] = (float)sinValue;

                    audioWaveTheta += sampleIncrement;
                }
            }

            return frame;
        }

        private unsafe AudioFrame GenerateAudioFrame(uint sampleCountPerChannel, uint channelCount, uint sampleRate)
        {
            uint totalSampleCount = sampleCountPerChannel * channelCount;
            uint bufferSize = totalSampleCount * sizeof(short);
            AudioFrame frame = new(bufferSize);
            frame.Duration = TimeSpan.FromSeconds((double)sampleCountPerChannel / sampleRate);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out _);

                // Float span since audio graph only supports float
                Span<short> dest = new(dataInBytes, (int)totalSampleCount);

                // VLC samples are interleaved, Windows expects data for each channel to be contiguous
                for (int i = 0; i < totalSampleCount; i++)
                {
                    dest[i] = _audioBuffer.Front();
                    _audioBuffer.PopFront();
                }
            }

            return frame;
        }

        private void DrainCb(IntPtr data)
        {

        }

        private void FlushCb(IntPtr data, long pts)
        {
            _inputNode?.DiscardQueuedFrames();
            lock (_audioBuffer)
            {
                _audioBuffer.Clear();
            }
        }

        private void ResumeCb(IntPtr data, long pts)
        {
            _audioGraph?.Start();
        }

        private void PauseCb(IntPtr data, long pts)
        {
            _audioGraph?.Stop();
        }

        private unsafe void PlayCb(IntPtr data, IntPtr samplesPtr, uint countPerChannel, long pts)
        {
            if (_audioGraph == null || _inputNode == null) return;
            // Assume VLC has the same number of channels as the audio graph
            uint channelCount = _inputNode.EncodingProperties.ChannelCount;
            uint sampleRate = _inputNode.EncodingProperties.SampleRate;

            // if (_inputNode.QueuedSampleCount > sampleRate * 3)
            //     return;

            uint sampleCount = countPerChannel * channelCount;
            uint bufferSize = sampleCount * sizeof(short);

            ReadOnlySpan<short> src = new((void*)samplesPtr, (int)sampleCount);
            lock (_audioBuffer)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    _audioBuffer.PushBack(src[i]);
                }

                return;
            }

            AudioFrame frame = new(bufferSize);
            frame.Duration = TimeSpan.FromSeconds((double)countPerChannel / sampleRate);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out _);

                // Float span since audio graph only supports float
                Span<short> dest = new(dataInBytes, (int)sampleCount);

                // Assume sample format is fixed to floating point with VlcPlayer.SetAudioFormat("FL32",...)
                // ReadOnlySpan<short> src = new((void*)samplesPtr, (int)sampleCount);

                // VLC samples are interleaved, Windows expects data for each channel to be contiguous
                // for (int i = 0; i < sampleCount; i++)
                // {
                //     // uint channel = i % channelCount;
                //     // uint offset = (i / channelCount) + (channel * countPerChannel);
                //     // dest[(int)offset] = (float)src[(int)i] / short.MaxValue;
                //     dest[i] = (float)src[i] / short.MaxValue;
                // }

                src.CopyTo(dest);
            }

            // _frameQueue.Enqueue(frame);
            // _audioGraph.Start();
            _inputNode.AddFrame(frame);
            try
            {
            }
            catch (InvalidOperationException)
            {
                // _inputNode.DiscardQueuedFrames();
                // _inputNode.AddFrame(frame);
                // _frameBuffer.Enqueue(frame);
            }
        }

        private void VlcPlayer_SeekableChanged(object sender, MediaPlayerSeekableChangedEventArgs e)
        {
            bool seekable = e.Seekable > 0;
            if (CanSeek == seekable) return;
            CanSeek = seekable;
            CanSeekChanged?.Invoke(this, EventArgs.Empty);
        }

        private void VlcPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            TimeSpan newValue = TimeSpan.FromMilliseconds(e.Time);
            if (newValue != _position)
            {
                TimeSpan oldValue = _position;
                _position = newValue;
                PositionChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(newValue, oldValue));
            }
        }

        private void VlcPlayer_EncounteredError(object sender, EventArgs e)
        {
            MediaFailed?.Invoke(this, EventArgs.Empty);
            PlaybackState = MediaPlaybackState.None;
        }

        private void VlcPlayer_ESSelected(object sender, MediaPlayerESSelectedEventArgs e)
        {
            if (PlaybackItem == null || e.Type != TrackType.Text) return;
            // VLC sometimes auto selects a subtitle track.
            // Update subtitle track index accordingly.
            int spu = e.Id;
            if (spu < 0) return;

            for (int i = 0; i < PlaybackItem.SubtitleTracks.Count; i++)
            {
                if (PlaybackItem.SubtitleTracks[i].VlcSpu == spu)
                {
                    PlaybackItem.SubtitleTracks.SelectedIndex = i;
                    break;
                }
            }
        }

        private void VlcPlayer_ChapterChanged(object sender, MediaPlayerChapterChangedEventArgs e)
        {
            if (PlaybackItem == null || e.Chapter < 0 || e.Chapter >= PlaybackItem.Chapters.Count)
            {
                Chapter = null;
                return;
            }

            Chapter = PlaybackItem.Chapters[e.Chapter];
        }

        private void VlcPlayer_ESAdded(object sender, MediaPlayerESAddedEventArgs e)
        {
            if (PlaybackItem == null || PlaybackState == MediaPlaybackState.Opening) return;
            if (e.Type == TrackType.Text)
            {
                PlaybackItem.SubtitleTracks.NotifyTrackAdded(e.Id);
            }
        }

        private void VlcPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            NaturalDuration = TimeSpan.FromMilliseconds(e.Length);
        }

        private void VlcPlayer_Playing(object sender, EventArgs e)
        {
            PlaybackState = MediaPlaybackState.Playing;
            if (!_updateMediaProperties) return;
            _updateMediaProperties = false;

            // Update video dimension
            uint px = 0, py = 0;
            VlcPlayer.Size(0, ref px, ref py);
            if (NaturalVideoWidth != px || NaturalVideoHeight != py)
            {
                NaturalVideoWidth = px;
                NaturalVideoHeight = py;
                NaturalVideoSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void VlcPlayer_EndReached(object sender, EventArgs e)
        {
            MediaEnded?.Invoke(this, EventArgs.Empty);
            PlaybackState = MediaPlaybackState.None;
            if (IsLoopingEnabled)
                Replay();
        }

        private void VlcPlayer_Opening(object sender, EventArgs e)
        {
            MediaOpened?.Invoke(this, EventArgs.Empty);
            NaturalDuration = TimeSpan.Zero;
            PlaybackState = MediaPlaybackState.Opening;
        }

        private void VlcPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            if (BufferingProgress == 0)
            {
                BufferingStarted?.Invoke(this, EventArgs.Empty);
                //PlaybackStateChanged?.Invoke(this, null);
            }

            BufferingProgress = e.Cache / 100d;
            BufferingProgressChanged?.Invoke(this, EventArgs.Empty);
            if (BufferingProgress == 1.0)
            {
                BufferingEnded?.Invoke(this, EventArgs.Empty);
                //PlaybackStateChanged?.Invoke(this, null);
                BufferingProgress = 0;
            }
        }

        private void RemoveItemHandlers(PlaybackItem item)
        {
            item.SubtitleTracks.SelectedIndexChanged -= SubtitleTracksOnSelectedIndexChanged;
            item.AudioTracks.SelectedIndexChanged -= AudioTracksOnSelectedIndexChanged;
        }

        private void RegisterItemHandlers(PlaybackItem item)
        {
            RemoveItemHandlers(item);
            item.SubtitleTracks.SelectedIndexChanged += SubtitleTracksOnSelectedIndexChanged;
            item.AudioTracks.SelectedIndexChanged += AudioTracksOnSelectedIndexChanged;
        }

        private void AudioTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            PlaybackAudioTrackList trackList = (PlaybackAudioTrackList)sender;
            VlcPlayer.SetAudioTrack(sender.SelectedIndex < 0 ? -1 : trackList[sender.SelectedIndex].VlcTrackId);
        }

        private void SubtitleTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            PlaybackSubtitleTrackList trackList = (PlaybackSubtitleTrackList)sender;
            VlcPlayer.SetSpu(sender.SelectedIndex < 0 ? -1 : trackList[sender.SelectedIndex].VlcSpu);
        }

        public void AddSubtitle(IStorageFile file, bool select = true)
        {
            if (PlaybackItem == null) return;
            string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "subtitle");
            VlcPlayer.AddSlave(MediaSlaveType.Subtitle, mrl, select);
        }

        public void Close()
        {
            VlcPlayer.Dispose();
        }

        public void Pause()
        {
            if (PlaybackState != MediaPlaybackState.Playing) return;
            VlcPlayer.Pause();
        }

        public void Play()
        {
            if (PlaybackItem?.Media == null) return;
            if (_readyToPlay)
            {
                _readyToPlay = false;
                VlcPlayer.Play(PlaybackItem.Media);
            }
            else
            {
                if (VlcPlayer.State == VLCState.Ended)
                    VlcPlayer.Stop();

                VlcPlayer.Play();
            }
        }

        public void StepBackwardOneFrame()
        {
            Position -= TimeSpan.FromSeconds(.042);
        }

        public void StepForwardOneFrame()
        {
            VlcPlayer.NextFrame();
        }

        private void Replay()
        {
            VlcPlayer.Stop();
            VlcPlayer.Play();
        }

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            if (args.Role == AudioDeviceRole.Default)
            {
                VlcPlayer.SetOutputDevice(args.Id);
            }
        }
    }
}
