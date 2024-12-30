#nullable enable

using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Screenbox.Core.Services
{
    public interface IWindowService
    {
        public event EventHandler<ViewModeChangedEventArgs>? ViewModeChanged;
        public WindowViewMode ViewMode { get; }
        public bool TryEnterFullScreen();
        public void ExitFullScreen();
        public Task<bool> TryExitCompactLayoutAsync();
        public Task<bool> TryEnterCompactLayoutAsync(Size viewSize);
        Size GetMaxWindowSize();
        double ResizeWindow(Size desiredSize, double scalar = 0);
        void HideCursor();
        void ShowCursor();
    }
}