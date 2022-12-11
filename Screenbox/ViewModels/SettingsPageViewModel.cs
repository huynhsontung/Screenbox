using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Services;
using Screenbox.Core;

namespace Screenbox.ViewModels
{
    internal sealed partial class SettingsPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        public SettingsPageViewModel(INavigationService navigationService)
        {
            _navigationViewDisplayMode = navigationService.DisplayMode;
            navigationService.DisplayModeChanged += NavigationServiceOnDisplayModeChanged;
        }

        private void NavigationServiceOnDisplayModeChanged(object sender, NavigationServiceDisplayModeChangedEventArgs e)
        {
            NavigationViewDisplayMode = e.NewValue;
        }
    }
}
