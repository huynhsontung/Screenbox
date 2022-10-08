#nullable enable

using System;

namespace Screenbox.Pages
{
    internal interface IContentFrame
    {
        object? FrameContent { get; }
        Type SourcePageType { get; }
        bool CanGoBack { get; }
        void GoBack();
        void Navigate(Type pageType, object? parameter);
    }
}
