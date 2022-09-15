using Microsoft.UI.Xaml.Controls;

namespace Screenbox.Core
{
    internal class NavigationServiceDisplayModeChangedEventArgs : ValueChangedEventArgs<NavigationViewDisplayMode>
    {
        public NavigationServiceDisplayModeChangedEventArgs(NavigationViewDisplayMode newValue, NavigationViewDisplayMode oldValue) : base(newValue, oldValue)
        {
        }
    }
}
