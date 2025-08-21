using Screenbox.Core.Enums;
using Screenbox.Pages;
using System;

namespace Screenbox.Helpers;
public static class EnumExtensions
{
    public static string GetNavPageFirstLevel(this LaunchPageOption launchPageOption)
    {
        return launchPageOption switch
        {
            LaunchPageOption.Home => "home",
            LaunchPageOption.Songs => "music",
            LaunchPageOption.Albums => "music",
            LaunchPageOption.Artists => "music",
            LaunchPageOption.VideoFolders => "videos",
            LaunchPageOption.AllVideos => "videos",
            LaunchPageOption.Network => "network",
            LaunchPageOption.PlayQueue => "queue",
            _ => throw new ArgumentOutOfRangeException(nameof(launchPageOption), launchPageOption, null),
        };
    }

    public static string GetNavPageSecondLevel(this LaunchPageOption launchPageOption)
    {
        return launchPageOption switch
        {
            LaunchPageOption.Songs => "songs",
            LaunchPageOption.Albums => "albums",
            LaunchPageOption.Artists => "artists",
            LaunchPageOption.VideoFolders => "folders",
            LaunchPageOption.AllVideos => "all",
            _ => throw new ArgumentOutOfRangeException(nameof(launchPageOption), launchPageOption, null),
        };
    }
}
