using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class SuspendingMessage : CollectionRequestMessage<Task>
    {
    }
}
