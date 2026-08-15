using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Screenbox.Core.Common;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Helpers;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ObservableAlbumGroup :
    ObservableCollection<AlbumViewModel>, IGrouping<string, AlbumViewModel>, IStringKey
{
    public string Key { get; }

    public ObservableAlbumGroup(string key) : base()
    {
        Key = key;
    }

    public ObservableAlbumGroup(string key, IEnumerable<AlbumViewModel> list) : base(list)
    {
        Key = key;
    }

    public ObservableAlbumGroup(IGrouping<string, AlbumViewModel> group) : base(group)
    {
        Key = group.Key;
    }

    public override string ToString()
    {
        return Key;
    }
}
