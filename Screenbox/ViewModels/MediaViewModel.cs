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

        [ObservableProperty] private bool _isPlaying;

        public MediaViewModel(object source)
        {
            Source = source;
        }

        public MediaViewModel(IStorageFile file)
        {
            Source = file;
            Title = file.Name;
            Location = file.Path;
        }
    }
}
