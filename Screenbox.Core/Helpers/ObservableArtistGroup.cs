using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Screenbox.Core.Common;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Helpers;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ObservableArtistGroup :
    ObservableCollection<ArtistViewModel>, IGrouping<string, ArtistViewModel>, IStringKey
{
    public string Key { get; }

    public ObservableArtistGroup(string key) : base()
    {
        Key = key;
    }

    public ObservableArtistGroup(string key, IEnumerable<ArtistViewModel> list) : base(list)
    {
        Key = key;
    }

    public ObservableArtistGroup(IGrouping<string, ArtistViewModel> group) : base(group)
    {
        Key = group.Key;
    }
}
