#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Pages;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Screenbox.Services
{
    internal sealed class NavigationService : INavigationService
    {
        public event EventHandler<NavigationServiceDisplayModeChangedEventArgs>? DisplayModeChanged;

        public muxc.NavigationViewDisplayMode DisplayMode
        {
            get => _displayMode;
            set
            {
                if (value == _displayMode) return;
                muxc.NavigationViewDisplayMode oldValue = _displayMode;
                _displayMode = value;
                DisplayModeChanged?.Invoke(this, new NavigationServiceDisplayModeChangedEventArgs(value, oldValue));
            }
        }

        private readonly Dictionary<Type, Type> _vmPageMapping;
        private muxc.NavigationViewDisplayMode _displayMode;

        public NavigationService(params KeyValuePair<Type,Type>[] mapping)
        {
            _vmPageMapping = new Dictionary<Type, Type>(mapping);
        }

        public void Navigate(Type vmType, object? parameter = null)
        {
            if (!_vmPageMapping.TryGetValue(vmType, out Type pageType)) return;

            Frame rootFrame = (Frame)Window.Current.Content;
            if (rootFrame.Content is IContentFrame page)
            {
                page.Navigate(pageType, parameter);
            }
        }

        public void NavigateParent(Type parentVmType, Type targetVmType, object? parameter = null)
        {
            if (!_vmPageMapping.TryGetValue(parentVmType, out Type parentPageType)) return;
            if (!_vmPageMapping.TryGetValue(targetVmType, out Type targetPageType)) return;

            Frame rootFrame = (Frame)Window.Current.Content;
            IContentFrame? page = rootFrame.Content as IContentFrame;
            while (page != null)
            {
                if (page.SourcePageType == parentPageType)
                {
                    page.Navigate(targetPageType, parameter);
                    break;
                }

                page = page.FrameContent as IContentFrame;
            }
        }
    }
}
