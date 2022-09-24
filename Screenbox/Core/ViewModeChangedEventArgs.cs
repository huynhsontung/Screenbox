using Screenbox.Services;

namespace Screenbox.Core
{
    internal sealed class ViewModeChangedEventArgs : ValueChangedEventArgs<WindowViewMode>
    {
        public ViewModeChangedEventArgs(WindowViewMode newValue, WindowViewMode oldValue) : base(newValue, oldValue)
        {
        }
    }
}
