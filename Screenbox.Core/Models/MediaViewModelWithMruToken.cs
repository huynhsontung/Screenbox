
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core
{
    public record MediaViewModelWithMruToken(string Token, MediaViewModel Media)
    {
        public string Token { get; } = Token;

        public MediaViewModel Media { get; } = Media;
    }
}
