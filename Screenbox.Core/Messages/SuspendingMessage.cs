using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages;

public partial class SuspendingMessage : CollectionRequestMessage<Task>
{
}
