using Windows.Foundation;

namespace Screenbox.Services
{
    internal interface IWindowService
    {
        double ResizeWindow(Size videoDimension, double scalar = 0);
        void HideCursor();
        void ShowCursor();
    }
}