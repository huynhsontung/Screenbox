#nullable enable

using System;
using System.Collections.Generic;
using Screenbox.Core.Enums;
using Windows.UI.Xaml;

namespace Screenbox.Core.Contexts;

internal sealed class NavigationContext
{
    internal Dictionary<Type, string> NavigationStates { get; } = new();
    internal Dictionary<string, object> PageStates { get; } = new();
    internal NavigationViewDisplayMode NavigationViewDisplayMode { get; set; }
    internal Thickness ScrollBarMargin { get; set; }
    internal Thickness FooterBottomPaddingMargin { get; set; }
    internal double FooterBottomPaddingHeight { get; set; }
}
