
namespace Screenbox.Core.ViewModels
{
    public record MediaViewModelWithMruToken(string Token, MediaViewModel Media)
    {
        public string Token { get; } = Token;

        public MediaViewModel Media { get; } = Media;
    }
}
