#nullable enable

using System;

namespace Screenbox.Core.Services
{
    public interface INavigationService
    {
        void Navigate(Type vmType, object? parameter = null);
        void NavigateChild(Type parentVmType, Type targetVmType, object? parameter = null);
        void NavigateExisting(Type vmType, object? parameter = null);
    }
}