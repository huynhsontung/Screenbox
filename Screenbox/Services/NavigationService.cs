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
    internal class NavigationService : INavigationService
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

        private readonly Dictionary<Type, string> _viewModelMappings;
        private muxc.NavigationViewDisplayMode _displayMode;

        public NavigationService(params KeyValuePair<Type,string>[] mappings)
        {
            _viewModelMappings = new Dictionary<Type, string>(mappings);
        }

        public void Navigate(Type viewModelType)
        {
            if (_viewModelMappings.TryGetValue(viewModelType, out string tag))
            {
                Frame rootFrame = (Frame)Window.Current.Content;
                if (rootFrame.Content is MainPage page)
                {
                    page.NavigateContentFrame(tag);
                }
            }
        }
    }
}
