using Windows.System;

namespace Screenbox.Core.Helpers;

public static class DispatcherQueueExtensions
{
    public static bool TryExecuteOrEnqueue(this DispatcherQueue dispatcherQueue, DispatcherQueueHandler action)
    {
        if (dispatcherQueue.HasThreadAccess)
        {
            action();
            return true;
        }
        else
        {
            return dispatcherQueue.TryEnqueue(action);
        }
    }
}
