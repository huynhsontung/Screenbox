using TUnit.Core;

namespace Screenbox.Core.Tests.Helpers;

public static class TestInitializer
{
    [BeforeEvery(HookType.Test)]
    public static void InitializeTest()
    {
        DispatcherQueueTestHelper.EnsureDispatcherQueue();
    }
}
