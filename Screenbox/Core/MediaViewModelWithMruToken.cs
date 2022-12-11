using Screenbox.ViewModels;

namespace Screenbox.Core
{
    internal record struct MediaViewModelWithMruToken(string Token, MediaViewModel Media)
    {
    }
}
