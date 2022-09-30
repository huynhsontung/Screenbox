using System.Collections.Generic;

namespace Screenbox.ViewModels
{
    internal sealed class ArtistViewModel
    {
        public List<MediaViewModel> RelatedSongs { get; }

        public string Name { get; }

        public ArtistViewModel(string artist)
        {
            Name = artist;
            RelatedSongs = new List<MediaViewModel>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
