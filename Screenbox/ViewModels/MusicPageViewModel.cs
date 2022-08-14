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
        public IEnumerable<IGrouping<string, MediaViewModel>>? Songs { get; private set; }

        private readonly IFilesService _filesService;

        public MusicPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            Messenger.Send(new PlayMediaMessage(media));
        }

        public async Task FetchSongsAsync()
        {
            IReadOnlyList<StorageFile> files = await _filesService.GetSongsFromLibraryAsync();
            MediaViewModel[] media = files.Select(f => new MediaViewModel(f)).ToArray();
            await Task.WhenAll(media.Select(m => m.LoadDetailsAsync()));
            Songs = media.GroupBy(GroupByFirstLetter);
        }

        private string GroupByFirstLetter(MediaViewModel media)
        {
            return char.IsLetter(media.Name, 0) ? media.Name.Substring(0, 1).ToUpper() : "#";
        }
    }
}
