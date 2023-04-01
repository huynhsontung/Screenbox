using System.Collections.Generic;

namespace Screenbox.Core.ViewModels
{
    public sealed class ArtistViewModel
    {
        public List<MediaViewModel> RelatedSongs { get; }

        public string Name { get; }

        public ArtistViewModel()
        {
            Name = string.Empty;
            RelatedSongs = new List<MediaViewModel>();
        }

        public ArtistViewModel(string artist) : this()
        {
            Name = artist;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
