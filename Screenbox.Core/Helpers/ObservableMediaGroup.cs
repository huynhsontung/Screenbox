using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Helpers;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ObservableMediaGroup : ObservableCollection<MediaViewModel>, IGrouping<string, MediaViewModel>
{
    public string Key { get; }

    public ObservableMediaGroup(string key) : base()
    {
        Key = key;
    }

    public ObservableMediaGroup(string key, IEnumerable<MediaViewModel> list) : base(list)
    {
        Key = key;
    }

    public ObservableMediaGroup(IGrouping<string, MediaViewModel> group) : base(group)
    {
        Key = group.Key;
    }
}
