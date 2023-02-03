using System.Collections.ObjectModel;
using System.Linq;

namespace Screenbox.ViewModels
{
    internal sealed class AlbumViewModel
    {
        public string Name { get; }

        public string Artist => string.IsNullOrEmpty(_albumArtist) && RelatedSongs.Count > 0
            ? RelatedSongs[0].Artists?.FirstOrDefault()?.Name ?? string.Empty
            : _albumArtist;

        public uint? Year { get; set; }

        public ObservableCollection<MediaViewModel> RelatedSongs { get; }

        private readonly string _albumArtist;

        public AlbumViewModel(string album, string albumArtist)
        {
            Name = album;
            _albumArtist = albumArtist;
            RelatedSongs = new ObservableCollection<MediaViewModel>();
        }

        public override string ToString()
        {
            return $"{Name};{Artist}";
        }
    }
}
