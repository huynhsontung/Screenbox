
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core
{
    public record struct MediaViewModelWithMruToken(string Token, MediaViewModel Media)
    {
    }
}
