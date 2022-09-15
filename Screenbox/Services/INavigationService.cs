#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal interface INavigationService
    {
        event EventHandler<NavigationServiceDisplayModeChangedEventArgs>? DisplayModeChanged;
        NavigationViewDisplayMode DisplayMode { get; set; }
        void Navigate(Type viewModelType);
    }
}