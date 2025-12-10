#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.Contexts;

public sealed class NavigationContext
{
    public Dictionary<Type, string> NavigationStates { get; } = new();
    public Dictionary<string, object> PageStates { get; } = new();
    public NavigationViewDisplayMode NavigationViewDisplayMode { get; set; }
    public Thickness ScrollBarMargin { get; set; }
    public Thickness FooterBottomPaddingMargin { get; set; }
    public double FooterBottomPaddingHeight { get; set; }
}
