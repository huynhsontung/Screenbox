
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models
{
    public record MediaViewModelWithMruToken(string Token, MediaViewModel Media)
    {
        public string Token { get; } = Token;

        public MediaViewModel Media { get; } = Media;
    }
}
