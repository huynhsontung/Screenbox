#nullable enable

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages;

public sealed class DeleteCurrentMediaFileRequestMessage : RequestMessage<Task<bool>>
{
}
