#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.Services
{
    public sealed class NavigationService : INavigationService
    {
        private readonly Dictionary<Type, Type> _vmPageMapping;

        public NavigationService(params KeyValuePair<Type, Type>[] mapping)
        {
            _vmPageMapping = new Dictionary<Type, Type>(mapping);
        }

        public bool TryGetPageType(Type vmType, out Type pageType)
        {
            return _vmPageMapping.TryGetValue(vmType, out pageType);
        }

        public void Navigate(Type vmType, object? parameter = null)
        {
            if (!_vmPageMapping.TryGetValue(vmType, out Type pageType)) return;

            Frame rootFrame = (Frame)Window.Current.Content;
            if (rootFrame.Content is IContentFrame page)
            {
                page.NavigateContent(pageType, parameter);
            }
        }

        public void NavigateChild(Type parentVmType, Type targetVmType, object? parameter = null)
        {
            if (!_vmPageMapping.TryGetValue(parentVmType, out Type parentPageType)) return;
            if (!_vmPageMapping.TryGetValue(targetVmType, out Type targetPageType)) return;

            Frame rootFrame = (Frame)Window.Current.Content;
            IContentFrame? page = rootFrame.Content as IContentFrame;
            while (page != null)
            {
                if (page.ContentSourcePageType == parentPageType && page.FrameContent is IContentFrame childPage)
                {
                    childPage.NavigateContent(targetPageType, parameter);
                    return;
                }

                page = page.FrameContent as IContentFrame;
            }
        }

        public void NavigateExisting(Type vmType, object? parameter = null)
        {
            if (!_vmPageMapping.TryGetValue(vmType, out Type pageType)) return;

            Frame rootFrame = (Frame)Window.Current.Content;
            IContentFrame? page = rootFrame.Content as IContentFrame;
            while (page != null)
            {
                if (page.ContentSourcePageType == pageType)
                {
                    page.NavigateContent(pageType, parameter);
                    break;
                }

                page = page.FrameContent as IContentFrame;
            }
        }
    }
}
