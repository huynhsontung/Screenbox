#nullable enable

using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;

namespace Screenbox.Core.Services
{
    public sealed class WindowService : IWindowService
    {
        public event EventHandler<ViewModeChangedEventArgs>? ViewModeChanged;

        public WindowViewMode ViewMode
        {
            get => State.ViewMode;
            private set
            {
                WindowViewMode oldValue = State.ViewMode;
                if (oldValue != value)
                {
                    State.ViewMode = value;
                    ViewModeChanged?.Invoke(this, new ViewModeChangedEventArgs(value, oldValue));
                }
            }
        }

        private readonly WindowState State;

        public WindowService(WindowState state)
        {
            State = state;
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

        public Size GetMaxWindowSize()
        {
            DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
            ApplicationView view = ApplicationView.GetForCurrentView();
            return GetMaxWindowSize(view, displayInformation);
        }

        private static Size GetMaxWindowSize(ApplicationView view, DisplayInformation displayInformation)
        {
            double maxWidth = displayInformation.ScreenWidthInRawPixels / displayInformation.RawPixelsPerViewPixel;
            double maxHeight = displayInformation.ScreenHeightInRawPixels / displayInformation.RawPixelsPerViewPixel - 48;
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                DisplayRegion? displayRegion = view.GetDisplayRegions()[0]; // Active display region
                maxWidth = displayRegion.WorkAreaSize.Width / displayInformation.RawPixelsPerViewPixel;
                maxHeight = displayRegion.WorkAreaSize.Height / displayInformation.RawPixelsPerViewPixel;
            }

            // Cannot use the full work area size. Subtract some padding.
            maxHeight -= 16;
            maxWidth -= 16;
            return new Size(maxWidth, maxHeight);
        }

        public double ResizeWindow(Size desiredSize, double scalar = 1)
        {
            if (scalar < 0 || desiredSize.IsEmpty) return -1;
            ApplicationView view = ApplicationView.GetForCurrentView();
            DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
            Size maxWindowSize = GetMaxWindowSize(view, displayInformation);
            double maxWidth = maxWindowSize.Width;
            double maxHeight = maxWindowSize.Height;

            if (scalar == 0)
            {
                double widthRatio = maxWidth / desiredSize.Width;
                double heightRatio = maxHeight / desiredSize.Height;
                scalar = Math.Min(widthRatio, heightRatio);
            }

            double aspectRatio = desiredSize.Width / desiredSize.Height;
            double newWidth = desiredSize.Width * scalar;
            if (newWidth > maxWidth) newWidth = maxWidth;
            double newHeight = newWidth / aspectRatio;
            scalar = newWidth / desiredSize.Width;
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
                State.Cursor = coreWindow.PointerCursor;
                coreWindow.PointerCursor = null;
            }
        }

        public void ShowCursor()
        {
            CoreWindow? coreWindow = Window.Current.CoreWindow;
            coreWindow.PointerCursor ??= State.Cursor;
        }
    }
}
