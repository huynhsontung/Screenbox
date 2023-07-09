#nullable enable

using System;

namespace Screenbox.Core;

public class NavigationMetadata
{
    public Type RootPageType { get; }

    public object? Parameter { get; }

    public NavigationMetadata(Type rootPageType, object? parameter)
    {
        RootPageType = rootPageType;
        Parameter = parameter;
    }
}
