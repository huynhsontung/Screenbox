using Screenbox.Core.Tests.Helpers;
using Windows.System;

namespace Screenbox.Core.Tests;

public class DispatcherQueueTests
{
    [Test]
    public async Task DispatcherQueue_ShouldNotBeNull_WhenInitializedByGlobalHook()
    {
        var queue = DispatcherQueue.GetForCurrentThread();
        await Assert.That(queue).IsNotNull();

        var timer = queue!.CreateTimer();
        await Assert.That(timer).IsNotNull();
    }
}
