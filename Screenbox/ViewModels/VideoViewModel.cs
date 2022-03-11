using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.ViewModels
{
    public class VideoViewModel
    {
        public string Title { get; set; }

        public BitmapImage Thumbnail { get; set; }

        public string Location { get; set; }

        public TimeSpan Duration { get; set; }

        public StorageFile OriginalFile { get; set; }
    }
}
