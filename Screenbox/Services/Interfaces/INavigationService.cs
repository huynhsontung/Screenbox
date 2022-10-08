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
        void Navigate(Type vmType, object? parameter = null);
        void NavigateParent(Type parentVmType, Type targetVmType, object? parameter = null);
    }
}