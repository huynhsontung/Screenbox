using Screenbox.Services;

namespace Screenbox.Core
{
    internal class ViewModeChangedEventArgs : ValueChangedEventArgs<WindowViewMode>
    {
        public ViewModeChangedEventArgs(WindowViewMode newValue, WindowViewMode oldValue) : base(newValue, oldValue)
        {
        }
    }
}
