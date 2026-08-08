using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Helpers;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ObservableAlbumMediaGroup :
    ObservableCollection<MediaViewModel>, IGrouping<AlbumViewModel, MediaViewModel>
{
    public AlbumViewModel Key { get; }

    public ObservableAlbumMediaGroup(AlbumViewModel key) : base()
    {
        Key = key;
    }

    public ObservableAlbumMediaGroup(AlbumViewModel key, IEnumerable<MediaViewModel> list) : base(list)
    {
        Key = key;
    }

    public ObservableAlbumMediaGroup(IGrouping<AlbumViewModel, MediaViewModel> group) : base(group)
    {
        Key = group.Key;
    }
}
