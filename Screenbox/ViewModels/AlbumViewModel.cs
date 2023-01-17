using System.Collections.Generic;

namespace Screenbox.ViewModels
{
    internal sealed class AlbumViewModel
    {
        public string Name { get; }

        public string Artist { get; }

        public List<MediaViewModel> RelatedSongs { get; }

        public AlbumViewModel(string album, string albumArtist)
        {
            Name = album;
            Artist = albumArtist;
            RelatedSongs = new List<MediaViewModel>();
        }

        public override string ToString()
        {
            return $"{Name};{Artist}";
        }
    }
}
