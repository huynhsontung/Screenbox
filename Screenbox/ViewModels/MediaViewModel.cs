#nullable enable

using System;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Screenbox.ViewModels
{
    internal partial class MediaViewModel : ObservableObject
    {
        public string? Title { get; set; }

        public BitmapImage? Thumbnail { get; set; }

        public string? Location { get; set; }

        public TimeSpan Duration { get; set; }

        public object Source { get; }

        public MediaType MediaType { get; set; }

        [ObservableProperty] private bool _isPlaying;

        public MediaViewModel(object source)
        {
            Source = source;
            if (source is Uri or string)
            {
                MediaType = MediaType.Network;
            }
        }

        public MediaViewModel(IStorageFile file)
        {
            Source = file;
            Title = file.Name;
            Location = file.Path;

            if (file.ContentType.StartsWith("video"))
            {
                MediaType = MediaType.Video;
            }

            if (file.ContentType.StartsWith("audio"))
            {
                MediaType = MediaType.Audio;
            }
        }
    }

    public enum MediaType
    {
        Unknown,
        Audio,
        Video,
        Network
    }
}
