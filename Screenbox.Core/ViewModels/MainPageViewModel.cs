using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Windows.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Core.Services;

namespace Screenbox.ViewModels
{
    public sealed partial class MainPageViewModel : ObservableRecipient,
        IRecipient<PlayerVisibilityChangedMessage>,
        IRecipient<NavigationViewDisplayModeRequestMessage>
    {
        [ObservableProperty] private bool _playerVisible;
        [ObservableProperty] private bool _shouldUseMargin;
        [ObservableProperty] private bool _isPaneOpen;
        [ObservableProperty] private string _searchQuery;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly ISearchService _searchService;
        private readonly INavigationService _navigationService;

        public MainPageViewModel(ISearchService searchService, INavigationService navigationService)
        {
            _searchService = searchService;
            _navigationService = navigationService;
            _searchQuery = string.Empty;
            IsActive = true;
        }

        public void Receive(PlayerVisibilityChangedMessage message)
        {
            PlayerVisible = message.Value == PlayerVisibilityState.Visible;
            ShouldUseMargin = message.Value != PlayerVisibilityState.Hidden;
        }

        public void Receive(NavigationViewDisplayModeRequestMessage message)
        {
            message.Reply(NavigationViewDisplayMode);
        }

        public void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string searchQuery = sender.Text.Trim();
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (searchQuery.Length > 0)
                {
                    SearchResult result = _searchService.SearchLocalLibrary(searchQuery);
                    sender.ItemsSource = GetSuggestItems(result, searchQuery);
                }
                else
                {
                    sender.ItemsSource = Array.Empty<object>();
                }
            }
        }

        public void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string searchQuery = args.QueryText.Trim();
            if (args.ChosenSuggestion == null && searchQuery.Length > 0)
            {
                SearchResult result = _searchService.SearchLocalLibrary(searchQuery);
                _navigationService.Navigate(typeof(SearchResultPageViewModel), result);
                SearchQuery = string.Empty;
                if (this.NavigationViewDisplayMode != NavigationViewDisplayMode.Expanded)
                {
                    IsPaneOpen = false;
                }
            }
        }

        public void AutoSuggestBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            switch (args.SelectedItem)
            {
                case MediaViewModel media:
                    Messenger.Send(new PlayMediaMessage(media));
                    break;
                case AlbumViewModel album:
                    _navigationService.Navigate(typeof(AlbumDetailsPageViewModel), album);
                    break;
                case ArtistViewModel artist:
                    _navigationService.Navigate(typeof(ArtistDetailsPageViewModel), artist);
                    break;
            }

            SearchQuery = string.Empty;
            if (this.NavigationViewDisplayMode != NavigationViewDisplayMode.Expanded)
            {
                IsPaneOpen = false;
            }
        }

        private IReadOnlyList<object> GetSuggestItems(SearchResult result, string searchQuery)
        {
            if (!result.HasItems) return Array.Empty<object>();
            IEnumerable<Tuple<string, object>> songs = result.Songs.Take(6).Select(s => new Tuple<string, object>(s.Name, s));
            IEnumerable<Tuple<string, object>> videos = result.Videos.Take(6).Select(v => new Tuple<string, object>(v.Name, v));
            IEnumerable<Tuple<string, object>> artists = result.Artists.Take(6).Select(a => new Tuple<string, object>(a.Name, a));
            IEnumerable<Tuple<string, object>> albums = result.Albums.Take(6).Select(a => new Tuple<string, object>(a.Name, a));
            IEnumerable<(double, object)> searchResults = songs.Concat(videos).Concat(artists).Concat(albums)
                .Select(t => (GetRanking(t.Item1, searchQuery), t.Item2))
                .OrderBy(t => t.Item1).Take(10);
            return searchResults.Select(t => t.Item2).ToArray();
        }

        private static double GetRanking(string text, string query)
        {
            int index = text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
            if (query.Contains(' '))
            {
                return index;
            }

            string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            double wordRank = words
                .Select(s => s.IndexOf(query, StringComparison.CurrentCultureIgnoreCase))
                .Where(i => i >= 0)
                .Average();
            return index * 0.1 + wordRank;

        }
    }
}
