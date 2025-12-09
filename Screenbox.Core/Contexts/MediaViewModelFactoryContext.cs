#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

internal sealed class MediaViewModelFactoryContext
{
    internal Dictionary<string, WeakReference<MediaViewModel>> References { get; } = new();
    internal int ReferencesCleanUpThreshold { get; set; } = 1000;
}
