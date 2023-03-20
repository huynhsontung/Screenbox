using Screenbox.Core.Services;

namespace Screenbox.Core.Events
{
    public sealed class ViewModeChangedEventArgs : ValueChangedEventArgs<WindowViewMode>
    {
        public ViewModeChangedEventArgs(WindowViewMode newValue, WindowViewMode oldValue) : base(newValue, oldValue)
        {
        }
    }
}
