#nullable enable

using System;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Screenbox.ViewModels
{
    internal partial class MediaViewModel : ObservableObject
    {
        public string Title { get; }

        public BitmapImage? Thumbnail { get; set; }

        public string Location { get; }

        public TimeSpan Duration { get; set; }

        public object Source { get; }

        [ObservableProperty] private bool _isPlaying;

        public MediaViewModel(string title, string path)
        {
            Title = title;
            Location = path;
            Source = path;
        }

        public MediaViewModel(Uri uri)
        {
            Title = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : uri.ToString();
            Location = uri.ToString();
            Source = uri;
        }

        public MediaViewModel(IStorageFile file)
        {
            Source = file;
            Title = file.Name;
            Location = file.Path;
        }
    }
}
