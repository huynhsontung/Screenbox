#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// A snapshot of the user's videos library.
/// Replace the entire instance when the library is updated rather than modifying the collection.
/// </summary>
public sealed class VideosLibrary
{
    public static readonly VideosLibrary Empty = new VideosLibrary(new List<MediaViewModel>());

    public VideosLibrary(IReadOnlyList<MediaViewModel> videos)
    {
        Videos = videos;
    }

    public IReadOnlyList<MediaViewModel> Videos { get; }

    /// <summary>Returns the first video whose <see cref="MediaViewModel.Location"/> matches <paramref name="location"/>.</summary>
    public MediaViewModel? FindByLocation(string location)
    {
        return Videos.FirstOrDefault(vm =>
            vm.Location.Equals(location, StringComparison.OrdinalIgnoreCase));
    }
}
