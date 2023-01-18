using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.ViewModels
{
    internal sealed partial class AlbumDetailsPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private AlbumViewModel _source;
    }
}
