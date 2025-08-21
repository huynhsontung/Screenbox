#nullable enable

using Windows.ApplicationModel.DataTransfer;

namespace Screenbox.Core.Messages;
public class DragDropMessage
{
    public DataPackageView Data { get; }

    public DragDropMessage(DataPackageView data)
    {
        Data = data;
    }
}
