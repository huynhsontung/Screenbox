using System.Threading.Tasks;
using Windows.Foundation;

namespace Screenbox.Services
{
    internal interface IWindowService
    {
        public bool IsCompact { get; }
        public Task ExitCompactLayout();
        public Task EnterCompactLayout(Size viewSize);
        double ResizeWindow(Size videoDimension, double scalar = 0);
        void HideCursor();
        void ShowCursor();
    }
}