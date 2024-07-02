using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.WinUI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Linq;

namespace Screenbox.Core.ViewModels
{
    public sealed class ArtistsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, ArtistViewModel> GroupedArtists { get; }

        private readonly ILibraryService _libraryService;

        public ArtistsPageViewModel(ILibraryService libraryService) : base(libraryService)
        {
            _libraryService = libraryService;
            GroupedArtists = new ObservableGroupedCollection<string, ArtistViewModel>();
            PopulateGroups();
        }

        public override void FetchContent()
        {
            // No need to run fetch async. HomePageViewModel should already called the method.
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            Songs = musicLibrary.Songs;

            var groupings = musicLibrary.Artists
                .OrderBy(a => a.Name, StringComparer.CurrentCulture)
                .GroupBy(artist => artist == musicLibrary.UnknownArtist
                    ? "\u2026"
                    : MediaGroupingHelpers.GetFirstLetterGroup(artist.Name))
                .ToList();
            GroupedArtists.SyncObservableGroups(groupings);

            // Progressively update when it's still loading
            if (_libraryService.IsLoadingMusic)
            {
                RefreshTimer.Debounce(FetchContent, TimeSpan.FromSeconds(5));
            }
            else
            {
                RefreshTimer.Stop();
            }
        }

        private void PopulateGroups()
        {
            foreach (string key in MediaGroupingHelpers.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedArtists.AddGroup(key);
            }
        }
    }
}
