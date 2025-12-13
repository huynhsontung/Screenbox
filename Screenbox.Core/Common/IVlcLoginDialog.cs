#nullable enable

using Screenbox.Core.Common;

namespace Screenbox.Core
{
    public interface IVlcLoginDialog : IDialog
    {
        string? Text { get; set; }
        string? Username { get; set; }
        string Password { get; set; }
        bool AskStoreCredential { get; set; }
        bool StoreCredential { get; set; }
    }
}
