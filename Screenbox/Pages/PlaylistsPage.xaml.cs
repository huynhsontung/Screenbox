#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlaylistsPage : Page
{
    internal PlaylistsPageViewModel ViewModel => (PlaylistsPageViewModel)DataContext;

    internal CommonViewModel Common { get; }

    public PlaylistsPage()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlaylistsPageViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
    }
}
