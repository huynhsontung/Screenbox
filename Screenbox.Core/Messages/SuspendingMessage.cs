using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public class SuspendingMessage : CollectionRequestMessage<Task>
    {
    }
}
