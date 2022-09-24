using Microsoft.UI.Xaml.Controls;

namespace Screenbox.Core
{
    internal sealed class NavigationServiceDisplayModeChangedEventArgs : ValueChangedEventArgs<NavigationViewDisplayMode>
    {
        public NavigationServiceDisplayModeChangedEventArgs(NavigationViewDisplayMode newValue, NavigationViewDisplayMode oldValue) : base(newValue, oldValue)
        {
        }
    }
}
