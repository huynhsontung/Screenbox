#nullable enable

using System;
using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

public sealed class MediaViewModelFactoryContext
{
    public Dictionary<string, WeakReference<MediaViewModel>> References { get; } = new();
    public int ReferencesCleanUpThreshold { get; set; } = 1000;
}
