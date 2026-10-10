using System.Threading.Tasks;
using Screenbox.Core.Helpers;

namespace Screenbox.Core.Tests.Helpers;

public class TouchpadHelperTests
{
    [Before(Test)]
    public void Setup()
    {
        TouchpadHelper.Reset();
    }

    [Test]
    public async Task IsTouchpadDelta_Zero_ReturnsFalse()
    {
        bool result = TouchpadHelper.IsTouchpadDelta(0);
        await Assert.That(result).IsFalse();
    }

    [Test]
    [Arguments(120)]
    [Arguments(-120)]
    [Arguments(240)]
    [Arguments(-240)]
    [Arguments(360)]
    [Arguments(-360)]
    public async Task IsTouchpadDelta_StandardMouseWheelDeltas_ReturnsFalse(int delta)
    {
        bool result = TouchpadHelper.IsTouchpadDelta(delta);
        await Assert.That(result).IsFalse();
    }

    [Test]
    [Arguments(1)]
    [Arguments(-1)]
    [Arguments(15)]
    [Arguments(-25)]
    [Arguments(60)]
    [Arguments(-119)]
    [Arguments(121)]
    [Arguments(-125)]
    public async Task IsTouchpadDelta_FineGrainedDeltas_ReturnsTrue(int delta)
    {
        bool result = TouchpadHelper.IsTouchpadDelta(delta);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsTouchpadDelta_ContinuousGestureWithWheelDeltaMultiple_ReturnsTrue()
    {
        // First event is granular (touchpad starts scrolling)
        bool first = TouchpadHelper.IsTouchpadDelta(15);
        await Assert.That(first).IsTrue();

        // Intermediate event hits exactly 120 during acceleration
        bool second = TouchpadHelper.IsTouchpadDelta(120);
        await Assert.That(second).IsTrue();

        // Following event continues gesture
        bool third = TouchpadHelper.IsTouchpadDelta(80);
        await Assert.That(third).IsTrue();
    }

    [Test]
    public async Task IsTouchpadDelta_AfterReset_StandardMouseWheelReturnsFalse()
    {
        TouchpadHelper.IsTouchpadDelta(15);
        TouchpadHelper.Reset();

        bool mouseWheel = TouchpadHelper.IsTouchpadDelta(120);
        await Assert.That(mouseWheel).IsFalse();
    }

    [Test]
    public async Task IsTouchpadDelta_DoubleOverload_BehavesCorrectly()
    {
        await Assert.That(TouchpadHelper.IsTouchpadDelta(120.0)).IsFalse();
        await Assert.That(TouchpadHelper.IsTouchpadDelta(-120.0)).IsFalse();
        await Assert.That(TouchpadHelper.IsTouchpadDelta(15.4)).IsTrue();
        await Assert.That(TouchpadHelper.IsTouchpadDelta(-22.8)).IsTrue();
    }
}
