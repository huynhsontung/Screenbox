#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Screenbox.Pages;

namespace Screenbox.Services
{
    internal sealed class NavigationService : INavigationService
    {
        private readonly Dictionary<Type, Type> _vmPageMapping;

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
                if (page.SourcePageType == parentPageType && page.FrameContent is IContentFrame childPage)
                {
                    childPage.Navigate(targetPageType, parameter);
                    break;
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
                if (page.SourcePageType == pageType)
                {
                    page.Navigate(pageType, parameter);
                    break;
                }

                page = page.FrameContent as IContentFrame;
            }
        }
    }
}
