#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Screenbox.Core;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;

namespace Screenbox.Core.Services
{
    public sealed class WindowService : IWindowService
    {
        public event EventHandler<ViewModeChangedEventArgs>? ViewModeChanged;

        public WindowViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                WindowViewMode oldValue = _viewMode;
                if (oldValue != value)
                {
                    _viewMode = value;
                    ViewModeChanged?.Invoke(this, new ViewModeChangedEventArgs(value, oldValue));
                }
            }
        }

        private CoreCursor? _cursor;
        private WindowViewMode _viewMode;

        public WindowService()
        {
            Window.Current.SizeChanged += OnWindowSizeChanged;
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (ViewMode == WindowViewMode.FullScreen && !view.IsFullScreenMode)
                ViewMode = WindowViewMode.Default;
        }

        public bool TryEnterFullScreen()
        {
            ApplicationView? view = ApplicationView.GetForCurrentView();
            if (view.TryEnterFullScreenMode())
            {
                ViewMode = WindowViewMode.FullScreen;
                return true;
            }

            return false;
        }

        public void ExitFullScreen()
        {
            ApplicationView? view = ApplicationView.GetForCurrentView();
            view?.ExitFullScreenMode();
            if (ViewMode == WindowViewMode.FullScreen)
                ViewMode = WindowViewMode.Default;
        }

        public async Task<bool> TryExitCompactLayoutAsync()
        {
            ApplicationView? view = ApplicationView.GetForCurrentView();
            if (await view.TryEnterViewModeAsync(ApplicationViewMode.Default))
            {
                if (ViewMode == WindowViewMode.Compact)
                    ViewMode = WindowViewMode.Default;
                return true;
            }

            return false;
        }

        public async Task<bool> TryEnterCompactLayoutAsync(Size viewSize)
        {
            ApplicationView? view = ApplicationView.GetForCurrentView();
            ViewModePreferences? preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            if (!viewSize.IsEmpty)
            {
                preferences.ViewSizePreference = ViewSizePreference.Custom;
                preferences.CustomSize = viewSize;
            }

            if (await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences))
            {
                ViewMode = WindowViewMode.Compact;
                return true;
            }

            return false;
        }

        public double ResizeWindow(Size videoDimension, double scalar = 0)
        {
            if (scalar < 0 || videoDimension.IsEmpty) return -1;
            DisplayInformation? displayInformation = DisplayInformation.GetForCurrentView();
            ApplicationView? view = ApplicationView.GetForCurrentView();
            double maxWidth = displayInformation.ScreenWidthInRawPixels / displayInformation.RawPixelsPerViewPixel;
            double maxHeight = displayInformation.ScreenHeightInRawPixels / displayInformation.RawPixelsPerViewPixel - 48;
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                DisplayRegion? displayRegion = view.GetDisplayRegions()[0];
                maxWidth = displayRegion.WorkAreaSize.Width / displayInformation.RawPixelsPerViewPixel;
                maxHeight = displayRegion.WorkAreaSize.Height / displayInformation.RawPixelsPerViewPixel;
            }

            maxHeight -= 16;
            maxWidth -= 16;

            if (scalar == 0)
            {
                double widthRatio = maxWidth / videoDimension.Width;
                double heightRatio = maxHeight / videoDimension.Height;
                scalar = Math.Min(widthRatio, heightRatio);
            }

            double aspectRatio = videoDimension.Width / videoDimension.Height;
            double newWidth = videoDimension.Width * scalar;
            if (newWidth > maxWidth) newWidth = maxWidth;
            double newHeight = newWidth / aspectRatio;
            scalar = newWidth / videoDimension.Width;
            if (view.TryResizeView(new Size(newWidth, newHeight)))
            {
                return scalar;
            }

            return -1;
        }

        public void HideCursor()
        {
            CoreWindow? coreWindow = Window.Current.CoreWindow;
            if (coreWindow.PointerCursor?.Type == CoreCursorType.Arrow)
            {
                _cursor = coreWindow.PointerCursor;
                coreWindow.PointerCursor = null;
            }
        }

        public void ShowCursor()
        {
            CoreWindow? coreWindow = Window.Current.CoreWindow;
            coreWindow.PointerCursor ??= _cursor;
        }
    }
}
