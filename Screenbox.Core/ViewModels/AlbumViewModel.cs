#nullable enable

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.ViewModels
{
    public sealed class AlbumViewModel
    {
        public string Name { get; }

        public string Artist => string.IsNullOrEmpty(_albumArtist) && RelatedSongs.Count > 0
            ? RelatedSongs[0].Artists?.FirstOrDefault()?.Name ?? string.Empty
            : _albumArtist;

        public uint? Year
        {
            get => _year;
            set
            {
                if (value > 0)
                {
                    _year = value;
                }
            }
        }

        public BitmapImage? AlbumArt => RelatedSongs.Count > 0 ? RelatedSongs[0].Thumbnail : null;

        public ObservableCollection<MediaViewModel> RelatedSongs { get; }

        private readonly string _albumArtist;
        private uint? _year;

        public AlbumViewModel(string album, string albumArtist)
        {
            Name = album;
            _albumArtist = albumArtist;
            RelatedSongs = new ObservableCollection<MediaViewModel>();
        }

        public async Task LoadAlbumArtAsync()
        {
            if (RelatedSongs.Count > 0)
            {
                await RelatedSongs[0].LoadThumbnailAsync();
            }
        }

        public override string ToString()
        {
            return $"{Name};{Artist}";
        }
    }
}
