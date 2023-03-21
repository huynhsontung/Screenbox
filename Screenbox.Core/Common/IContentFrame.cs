#nullable enable

using System;

namespace Screenbox.Core
{
    public interface IContentFrame
    {
        object? FrameContent { get; }
        Type ContentSourcePageType { get; }
        bool CanGoBack { get; }
        void GoBack();
        void NavigateContent(Type pageType, object? parameter);
    }
}
