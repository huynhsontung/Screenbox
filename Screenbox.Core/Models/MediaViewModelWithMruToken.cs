using Screenbox.ViewModels;

namespace Screenbox.Core
{
    public record struct MediaViewModelWithMruToken(string Token, MediaViewModel Media)
    {
    }
}
