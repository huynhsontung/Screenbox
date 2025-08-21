#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class MainPageViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<PlayerVisibilityState>>,
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
        private readonly ILibraryService _libraryService;

        public MainPageViewModel(ISearchService searchService, INavigationService navigationService,
            ILibraryService libraryService)
        {
            _searchService = searchService;
            _navigationService = navigationService;
            _libraryService = libraryService;
            _searchQuery = string.Empty;
            _criticalErrorMessage = string.Empty;
            IsActive = true;
        }

        public void Receive(CriticalErrorMessage message)
        {
            HasCriticalError = true;
            CriticalErrorMessage = message.Message;
        }

        public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
        {
            PlayerVisible = message.NewValue == PlayerVisibilityState.Visible;
            ShouldUseMargin = message.NewValue != PlayerVisibilityState.Hidden;
        }

        public void Receive(NavigationViewDisplayModeRequestMessage message)
        {
            message.Reply(NavigationViewDisplayMode);
        }

        public bool TryGetPageTypeFromParameter(object? parameter, out Type pageType)
        {
            pageType = typeof(object);
            return parameter is NavigationMetadata metadata &&
                   _navigationService.TryGetPageType(metadata.RootViewModelType, out pageType);
        }

        public void ProcessGamepadKeyDown(KeyRoutedEventArgs args)
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
                case VirtualKey.GamepadView when !PlayerVisible:
                    Messenger.Send(new TogglePlayerVisibilityMessage());
                    break;
                default:
                    return;
            }

            if (volumeChange != 0)
            {
                int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeChange, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volume));
            }

            args.Handled = true;
        }

        public void OnDrop(DataPackageView data)
        {
            Messenger.Send(new DragDropMessage(data));
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
            }
            else
            {
                switch (args.ChosenSuggestion)
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
                    default:
                        return;
                }
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

        public Task FetchLibraries()
        {
            List<Task> tasks = new() { FetchMusicLibraryAsync(), FetchVideosLibraryAsync() };
            return Task.WhenAll(tasks);
        }

        private async Task FetchMusicLibraryAsync()
        {
            try
            {
                await _libraryService.FetchMusicAsync();
            }
            catch (UnauthorizedAccessException)
            {
                Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Music));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(null, e.Message));
                LogService.Log(e);
            }
        }

        private async Task FetchVideosLibraryAsync()
        {
            try
            {
                await _libraryService.FetchVideosAsync();
            }
            catch (UnauthorizedAccessException)
            {
                Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Videos));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(null, e.Message));
                LogService.Log(e);
            }
        }
    }
}
