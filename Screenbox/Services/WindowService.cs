#nullable enable

using System;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Screenbox.Services
{
    internal class WindowService : IWindowService
    {
        private CoreCursor? _cursor;

        public double ResizeWindow(Size videoDimension, double scalar = 0)
        {
            if (scalar < 0 || videoDimension.IsEmpty) return -1;
            var displayInformation = DisplayInformation.GetForCurrentView();
            var view = ApplicationView.GetForCurrentView();
            var maxWidth = displayInformation.ScreenWidthInRawPixels / displayInformation.RawPixelsPerViewPixel;
            var maxHeight = displayInformation.ScreenHeightInRawPixels / displayInformation.RawPixelsPerViewPixel - 48;
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                var displayRegion = view.GetDisplayRegions()[0];
                maxWidth = displayRegion.WorkAreaSize.Width / displayInformation.RawPixelsPerViewPixel;
                maxHeight = displayRegion.WorkAreaSize.Height / displayInformation.RawPixelsPerViewPixel;
            }

            maxHeight -= 16;
            maxWidth -= 16;

            if (scalar == 0)
            {
                var widthRatio = maxWidth / videoDimension.Width;
                var heightRatio = maxHeight / videoDimension.Height;
                scalar = Math.Min(widthRatio, heightRatio);
            }

            var aspectRatio = videoDimension.Width / videoDimension.Height;
            var newWidth = videoDimension.Width * scalar;
            if (newWidth > maxWidth) newWidth = maxWidth;
            var newHeight = newWidth / aspectRatio;
            scalar = newWidth / videoDimension.Width;
            if (view.TryResizeView(new Size(newWidth, newHeight)))
            {
                return scalar;
            }

            return -1;
        }

        public void HideCursor()
        {
            var coreWindow = Window.Current.CoreWindow;
            if (coreWindow.PointerCursor?.Type == CoreCursorType.Arrow)
            {
                _cursor = coreWindow.PointerCursor;
                coreWindow.PointerCursor = null;
            }
        }

        public void ShowCursor()
        {
            var coreWindow = Window.Current.CoreWindow;
            coreWindow.PointerCursor ??= _cursor;
        }
    }
}
