using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class MainPageViewModel : ObservableRecipient,
        IRecipient<PlayerVisibilityChangedMessage>,
        IRecipient<NavigationViewDisplayModeRequestMessage>,
        IRecipient<CriticalErrorMessage>
    {
        [ObservableProperty] private bool _playerVisible;
        [ObservableProperty] private bool _shouldUseMargin;
        [ObservableProperty] private bool _isPaneOpen;
        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] private string _criticalErrorMessage;
        [ObservableProperty] private bool _hasCriticalError;

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
            _criticalErrorMessage = string.Empty;
            IsActive = true;
        }

        public void Receive(CriticalErrorMessage message)
        {
            HasCriticalError = true;
            CriticalErrorMessage = message.Message;
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

        public void ProcessGamepadKeyDown(object sender, KeyRoutedEventArgs args)
        {
            // All Gamepad keys are in the range of [195, 218]
            if ((int)args.Key < 195 || (int)args.Key > 218) return;
            PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
            if (playlist.ActiveItem == null) return;
            int volumeChange = 0;
            switch (args.Key)
            {
                case VirtualKey.GamepadRightThumbstickLeft:
                case VirtualKey.GamepadLeftShoulder:
                    Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(-5000));
                    break;
                case VirtualKey.GamepadRightThumbstickRight:
                case VirtualKey.GamepadRightShoulder:
                    Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(5000));
                    break;
                case VirtualKey.GamepadLeftTrigger when PlayerVisible:
                    Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(-30_000));
                    break;
                case VirtualKey.GamepadRightTrigger when PlayerVisible:
                    Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(30_000));
                    break;
                case VirtualKey.GamepadRightThumbstickUp:
                    volumeChange = 2;
                    break;
                case VirtualKey.GamepadRightThumbstickDown:
                    volumeChange = -2;
                    break;
                case VirtualKey.GamepadX:
                    Messenger.Send(new TogglePlayPauseMessage(true));
                    break;
                case VirtualKey.GamepadView:
                    Messenger.Send(new TogglePlayerVisibilityMessage());
                    break;
                default:
                    return;
            }

            if (volumeChange != 0)
            {
                int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeChange, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volume, false));
            }

            args.Handled = true;
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
