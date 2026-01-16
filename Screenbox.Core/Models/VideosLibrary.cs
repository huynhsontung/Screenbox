using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

public class VideosLibrary
{
    public List<MediaViewModel> Videos { get; }

    public VideosLibrary() : this(new List<MediaViewModel>()) { }

    public VideosLibrary(List<MediaViewModel> videos)
    {
        Videos = videos;
    }
}
