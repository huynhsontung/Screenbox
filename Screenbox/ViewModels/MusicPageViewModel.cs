#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private List<IGrouping<string, MediaViewModel>>? _groupedSongs;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly IFilesService _filesService;
        private readonly INavigationService _navigationService;
        private List<MediaViewModel>? _songs;

        public MusicPageViewModel(IFilesService filesService, INavigationService navigationService)
        {
            _filesService = filesService;
            _navigationService = navigationService;
            _navigationViewDisplayMode = navigationService.DisplayMode;

            navigationService.DisplayModeChanged += NavigationServiceOnDisplayModeChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        private void NavigationServiceOnDisplayModeChanged(object sender, NavigationServiceDisplayModeChangedEventArgs e)
        {
            NavigationViewDisplayMode = e.NewValue;
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            if (_songs == null) return;
            Messenger.Send(new QueuePlaylistMessage(_songs, media));
        }

        [RelayCommand]
        private void ShuffleAndPlay()
        {
            if (_songs == null) return;
            Random rnd = new();
            List<MediaViewModel> shuffledList = _songs.OrderBy(_ => rnd.Next()).ToList();
            Messenger.Send(new QueuePlaylistMessage(shuffledList, shuffledList[0]));
        }

        public async Task FetchSongsAsync()
        {
            if (GroupedSongs != null) return;
            IReadOnlyList<StorageFile> files = await _filesService.GetSongsFromLibraryAsync();
            List<IGrouping<string, MediaViewModel>> groupedSongs =
                files.Select(f => new MediaViewModel(f)).GroupBy(GroupByFirstLetter).ToList();
            GroupedSongs = groupedSongs;
            await Task.Run(() =>
            {
                _songs = groupedSongs.SelectMany(models => models.ToArray()).ToList();
            }).ConfigureAwait(false);
        }

        private string GroupByFirstLetter(MediaViewModel media)
        {
            return char.IsLetter(media.Name, 0) ? media.Name.Substring(0, 1).ToUpper() : "#";
        }
    }
}
