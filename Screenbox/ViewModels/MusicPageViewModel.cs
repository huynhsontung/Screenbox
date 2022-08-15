#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class MusicPageViewModel : ObservableRecipient
    {
        public List<IGrouping<string, MediaViewModel>>? GroupedSongs { get; private set; }

        private readonly IFilesService _filesService;
        private List<MediaViewModel>? _songs;

        public MusicPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
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
            List<MediaViewModel> media = _songs = files.Select(f => new MediaViewModel(f)).ToList();
            await Task.WhenAll(media.Select(m => m.LoadDetailsAsync()));
            GroupedSongs = media.GroupBy(GroupByFirstLetter).ToList();
        }

        private string GroupByFirstLetter(MediaViewModel media)
        {
            return char.IsLetter(media.Name, 0) ? media.Name.Substring(0, 1).ToUpper() : "#";
        }
    }
}
