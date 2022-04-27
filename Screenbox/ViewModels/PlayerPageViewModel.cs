#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Win32.SafeHandles;
using Screenbox.Converters;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class PlayerPageViewModel : ObservableRecipient, IRecipient<UpdateStatusMessage>, IDisposable
    {
        [ObservableProperty]
        private string _mediaTitle;

        [ObservableProperty]
        private Size _viewSize;

        [ObservableProperty]
        private bool _isFullscreen;

        [ObservableProperty]
        private bool _controlsHidden;

        [ObservableProperty]
        private bool _isCompact;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private bool _zoomToFit;

        [ObservableProperty]
        private bool _bufferingVisible;

        [ObservableProperty]
        private NotificationRaisedEventArgs? _notification;

        [ObservableProperty]
        private bool _videoViewFocused;

        [ObservableProperty]
        private bool _playerHidden;

        public ObservablePlayer MediaPlayer { get; }

        public object? ToBeOpened { get; set; }

        public MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _controlsVisibilityTimer;
        private readonly DispatcherQueueTimer _statusMessageTimer;
        private readonly DispatcherQueueTimer _notificationTimer;
        private readonly IFilesService _filesService;
        private readonly INotificationService _notificationService;
        private readonly IPlaylistService _playlistService;
        private readonly IWindowService _windowService;
        private readonly IMediaPlayerService _mediaPlayerService;
        private LibVLC? _libVlc;
        private MediaHandle? _mediaHandle;
        private bool _visibilityOverride;
        private StorageFile? _savedFrame;

        public PlayerPageViewModel(
            ObservablePlayer player,
            IMediaPlayerService mediaPlayerService,
            IWindowService windowService,
            IFilesService filesService,
            INotificationService notificationService,
            IPlaylistService playlistService)
        {
            MediaPlayer = player;
            MediaPlayer.PropertyChanged += MediaPlayerOnPropertyChanged;
            _mediaPlayerService = mediaPlayerService;
            _windowService = windowService;
            _filesService = filesService;
            _notificationService = notificationService;
            _playlistService = playlistService;
            _mediaTitle = string.Empty;
            _notificationService.NotificationRaised += OnNotificationRaised;
            _notificationService.ProgressUpdated += OnProgressUpdated;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
            _statusMessageTimer = _dispatcherQueue.CreateTimer();
            _notificationTimer = _dispatcherQueue.CreateTimer();

            PropertyChanged += OnPropertyChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        [ICommand]
        private async Task SaveSnapshot()
        {
            if (VlcPlayer == null || !VlcPlayer.WillPlay) return;
            try
            {
                StorageFile file = _savedFrame = await _filesService.SaveSnapshot(VlcPlayer);
                ShowNotification(new NotificationRaisedEventArgs
                {
                    Level = NotificationLevel.Success,
                    Title = "Frame saved",
                    LinkText = file.Name
                }, 8);
            }
            catch (Exception e)
            {
                ShowNotification(new NotificationRaisedEventArgs
                {
                    Level = NotificationLevel.Error,
                    Title = "Failed to save frame",
                    Message = e.Message
                }, 8);

                // TODO: track error
            }
        }

        public async void OpenSaveFolder()
        {
            StorageFile? savedFrame = _savedFrame;
            if (savedFrame != null)
            {
                StorageFolder? saveFolder = await savedFrame.GetParentAsync();
                FolderLauncherOptions options = new();
                options.ItemsToSelect.Add(savedFrame);
                await Launcher.LaunchFolderAsync(saveFolder, options);
            }
        }

        public void Receive(UpdateStatusMessage message)
        {
            _dispatcherQueue.TryEnqueue(() => ShowStatusMessage(message.Value));
        }

        private void OnProgressUpdated(object sender, ProgressUpdatedEventArgs e)
        {
            LogService.Log(e.Title);
            LogService.Log(e.Text);
            LogService.Log(e.Value);
        }

        private void OnNotificationRaised(object sender, NotificationRaisedEventArgs e)
        {
            ShowNotification(e);
        }

        private void ShowNotification(NotificationRaisedEventArgs notification, int ttl = default)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Notification = notification;
            });

            if (ttl <= 0) return;

            void InvalidateNotification()
            {
                if (Notification == notification)
                {
                    Notification = null;
                }
            }

            _notificationTimer.Debounce(InvalidateNotification, TimeSpan.FromSeconds(ttl));
        }

        private void ChangeVolume(double changeAmount)
        {
            ChangeVolumeMessage message = new(changeAmount, isOffset: true);
            Messenger.Send(message);
        }

        [ICommand]
        private async Task ToggleCompactLayout()
        {
            ApplicationView? view = ApplicationView.GetForCurrentView();
            if (IsCompact)
            {
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.Default))
                {
                    IsCompact = false;
                }
            }
            else
            {
                ViewModePreferences? preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.ViewSizePreference = ViewSizePreference.Custom;
                preferences.CustomSize = new Size(240 * (_mediaPlayerService.NumericAspectRatio ?? 1), 240);
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences))
                {
                    IsCompact = true;
                }
            }
        }

        // TODO: Move to its own MediaService/PlaylistService
        [ICommand]
        private void Open(object? value)
        {
            if (_libVlc == null)
            {
                ToBeOpened = value;
                return;
            }

            MediaHandle? mediaHandle = null;
            Uri? uri = null;

            if (value is StorageFile file)
            {
                uri = new Uri(file.Path);
                if (file.Provider.Id == "network")
                {
                    Media media = new(_libVlc, uri);
                    mediaHandle = new MediaHandle(media, uri);
                }
                else
                {
                    try
                    {
                        SafeFileHandle? handle = file.CreateSafeFileHandle(FileAccess.Read, FileShare.Read, FileOptions.RandomAccess);
                        if (handle == null) return;
                        FileStream stream = new(handle, FileAccess.Read);
                        StreamMediaInput streamInput = new(stream);
                        Media media = new(_libVlc, streamInput);
                        mediaHandle = new MediaHandle(media, uri)
                        {
                            FileHandle = handle,
                            Stream = stream,
                            StreamInput = streamInput
                        };
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ShowNotification(new NotificationRaisedEventArgs
                        {
                            Level = NotificationLevel.Error,
                            Title = "Cannot open file",
                            Message = "Access denied"
                        }, 8);
                        return;
                    }
                }
            }

            if (value is string str)
            {
                Uri.TryCreate(str, UriKind.Absolute, out uri);
                value = uri;
            }

            if (value is Uri uri1)
            {
                uri = uri1;
                Media media = new(_libVlc, uri);
                mediaHandle = new MediaHandle(media, uri);
            }

            if (mediaHandle == null)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                PlayerHidden = false;
                if (uri == null) return;
                MediaTitle = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
            });

            MediaHandle? oldMediaHandle = _mediaHandle;
            _mediaHandle = mediaHandle;

            mediaHandle.Media.ParsedChanged += OnMediaParsed;
            _mediaPlayerService.Play(mediaHandle.Media);

            oldMediaHandle?.Dispose();
        }

        private void OnMediaParsed(object sender, MediaParsedChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (ResizeWindow(1)) return;
                ResizeWindow();
            });
        }

        public void SetPlaybackSpeed(float speed)
        {
            _mediaPlayerService.Rate = speed;
        }

        [ICommand]
        private void Fullscreen(bool value)
        {
            ApplicationView? view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode && !value)
            {
                view.ExitFullScreenMode();
            }

            if (!view.IsFullScreenMode && value)
            {
                view.TryEnterFullScreenMode();
            }

            IsFullscreen = view.IsFullScreenMode;
        }

        public void ToggleFullscreen() => Fullscreen(!IsFullscreen);

        public void OnInitialized(object sender, InitializedEventArgs e)
        {
            _libVlc = InitializeLibVlc(e.SwapChainOptions);
            _mediaPlayerService.InitVlcPlayer(_libVlc);

            Open(ToBeOpened);
        }

        private LibVLC InitializeLibVlc(string[] swapChainOptions)
        {
            string[] options = new string[swapChainOptions.Length + 1];
            options[0] = "--no-osd";
            swapChainOptions.CopyTo(options, 1);
            LibVLC libVlc = new(true, options);
            _notificationService.SetVLCDiaglogHandlers(libVlc);
            LogService.RegisterLibVLCLogging(libVlc);
            return libVlc;
        }

        private void MediaPlayerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ObservablePlayer mediaPlayer = (ObservablePlayer)sender;
            switch (e.PropertyName)
            {
                case nameof(ObservablePlayer.State):
                    if (ControlsHidden && mediaPlayer.State != VLCState.Playing)
                    {
                        ShowControls();
                    }

                    if (!ControlsHidden && mediaPlayer.State == VLCState.Playing)
                    {
                        DelayHideControls();
                    }

                    break;
            }
        }

        public void OnBackRequested()
        {
            PlayerHidden = true;
            if (MediaPlayer.IsPlaying)
            {
                _mediaPlayerService.Pause();
            }
        }

        private void ShowStatusMessage(string? message)
        {
            StatusMessage = message;
            if (_overrideStatusTimeout || message == null) return;
            _statusMessageTimer.Debounce(() => StatusMessage = null, TimeSpan.FromSeconds(1));
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ZoomToFit):
                    SetCropGeometry(ViewSize);
                    break;

                case nameof(VideoViewFocused):
                    if (VideoViewFocused)
                    {
                        DelayHideControls();
                    }
                    else
                    {
                        ShowControls();
                    }
                    break;
            }
        }

        public void Dispose()
        {
            _controlsVisibilityTimer.Stop();
            _statusMessageTimer.Stop();

            _mediaHandle?.Dispose();
            _libVlc?.Dispose();
        }

        [ICommand]
        private void PlayPause()
        {
            if (MediaPlayer.State == VLCState.Ended)
            {
                _mediaPlayerService.Replay();
                return;
            }

            _mediaPlayerService.Pause();
        }

        private void Seek(long amount)
        {
            if (VlcPlayer?.IsSeekable ?? false)
            {
                if (MediaPlayer.State == VLCState.Ended && amount > 0) return;
                _mediaPlayerService.Seek(amount);
                ShowStatusMessage($"{HumanizedDurationConverter.Convert(VlcPlayer.Time)} / {HumanizedDurationConverter.Convert(VlcPlayer.Length)}");
            }
        }

        public bool JumpFrame(bool previous = false)
        {
            if (MediaPlayer.State == VLCState.Paused && (VlcPlayer?.IsSeekable ?? false))
            {
                if (previous)
                {
                    _mediaPlayerService.Seek(-_mediaPlayerService.FrameDuration);
                }
                else
                {
                    _mediaPlayerService.NextFrame();
                }

                return true;
            }

            return false;
        }

        public void ToggleControlsVisibility()
        {
            if (ControlsHidden)
            {
                ShowControls();
                DelayHideControls();
            }
            else if (MediaPlayer.IsPlaying && !_visibilityOverride)
            {
                HideControls();
                // Keep hiding even when pointer moved right after
                OverrideVisibilityChange();
            }
        }

        private bool ResizeWindow(double scalar = 0)
        {
            if (scalar < 0 || IsCompact) return false;
            Size videoDimension = _mediaPlayerService.Dimension;
            double actualScalar = _windowService.ResizeWindow(videoDimension, scalar);
            if (actualScalar > 0)
            {
                ShowStatusMessage($"Scale {actualScalar * 100:0.##}%");
                return true;
            }

            return false;
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs args)
        {
            ViewSize = args.NewSize;
            SetCropGeometry(ViewSize);
        }

        private void SetCropGeometry(Size size)
        {
            if (!ZoomToFit && _mediaPlayerService.CropGeometry == null) return;
            _mediaPlayerService.CropGeometry = ZoomToFit ? $"{size.Width}:{size.Height}" : null;
        }

        public void OnPointerMoved()
        {
            if (_visibilityOverride) return;
            if (ControlsHidden)
            {
                ShowControls();
            }

            if (Messenger.Send<ChangeSeekBarInteractionRequestMessage>()) return;
            DelayHideControls();
        }

        public void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null) e.DragUIOverride.Caption = "Open";
        }

        public async void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    Open(items[0]);
                    return;
                }
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                Uri? uri = await e.DataView.GetWebLinkAsync();
                if (uri.IsFile)
                {
                    Open(uri);
                    return;
                }
            }
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            ChangeVolume(mouseWheelDelta / 25.0);
        }

        public string GetChapterName(string? nullableName) => string.IsNullOrEmpty(nullableName)
            ? $"Chapter {VlcPlayer?.Chapter + 1}"
            : nullableName ?? string.Empty;

        public void ProcessKeyboardAccelerators(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            long seekAmount = 0;
            int volumeChange = 0;
            int direction = 0;
            VirtualKey key = sender.Key;

            switch (key)
            {
                case VirtualKey.Left when VideoViewFocused:
                case VirtualKey.J:
                    direction = -1;
                    break;
                case VirtualKey.Right when VideoViewFocused:
                case VirtualKey.L:
                    direction = 1;
                    break;
                case VirtualKey.Up when VideoViewFocused:
                    volumeChange = 10;
                    break;
                case VirtualKey.Down when VideoViewFocused:
                    volumeChange = -10;
                    break;
                case VirtualKey.NumberPad0:
                case VirtualKey.NumberPad1:
                case VirtualKey.NumberPad2:
                case VirtualKey.NumberPad3:
                case VirtualKey.NumberPad4:
                case VirtualKey.NumberPad5:
                case VirtualKey.NumberPad6:
                case VirtualKey.NumberPad7:
                case VirtualKey.NumberPad8:
                case VirtualKey.NumberPad9:
                    _mediaPlayerService.SetTime((VlcPlayer?.Length ?? 0) * (0.1 * (key - VirtualKey.NumberPad0)));
                    break;
                case VirtualKey.Number0:
                case VirtualKey.Number1:
                case VirtualKey.Number2:
                case VirtualKey.Number3:
                case VirtualKey.Number4:
                case VirtualKey.Number5:
                case VirtualKey.Number6:
                case VirtualKey.Number7:
                case VirtualKey.Number8:
                    ResizeWindow(0.25 * (key - VirtualKey.Number0));
                    return;
                case VirtualKey.Number9:
                    ResizeWindow(4);
                    return;
                case (VirtualKey)190:   // Period (".")
                    JumpFrame(false);
                    return;
                case (VirtualKey)188:   // Comma (",")
                    JumpFrame(true);
                    return;
            }

            switch (sender.Modifiers)
            {
                case VirtualKeyModifiers.Control:
                    seekAmount = 10000;
                    break;
                case VirtualKeyModifiers.Shift:
                    seekAmount = 1000;
                    break;
                case VirtualKeyModifiers.None:
                    seekAmount = 5000;
                    break;
            }

            if (seekAmount * direction != 0)
            {
                Seek(seekAmount * direction);
            }

            if (volumeChange != 0)
            {
                ChangeVolume(volumeChange);
            }
        }

        private void ShowControls()
        {
            _windowService.ShowCursor();
            ControlsHidden = false;
        }

        private void HideControls()
        {
            ControlsHidden = true;
            _windowService.HideCursor();
        }

        private void DelayHideControls()
        {
            _controlsVisibilityTimer.Debounce(() =>
            {
                if (MediaPlayer.IsPlaying && VideoViewFocused)
                {
                    HideControls();

                    // Workaround for PointerMoved is raised when show/hide cursor
                    OverrideVisibilityChange();
                }
            }, TimeSpan.FromSeconds(3));
        }

        private void OverrideVisibilityChange(int delay = 400)
        {
            _visibilityOverride = true;
            Task.Delay(delay).ContinueWith(_ => _visibilityOverride = false);
        }
    }
}
