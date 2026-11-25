using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;

namespace Screenbox.Core.Tests.Helpers;

public class VideoTransformHelperTests
{
    [Test]
    [Arguments(0, 0)]
    [Arguments(90, 90)]
    [Arguments(180, 180)]
    [Arguments(270, 270)]
    [Arguments(360, 0)]
    [Arguments(450, 90)]
    [Arguments(-90, 270)]
    [Arguments(-180, 180)]
    [Arguments(-270, 90)]
    [Arguments(45, 0)]
    public async Task NormalizeRotation_ReturnsExpectedDegrees(int input, int expected)
    {
        await Assert.That(VideoTransformHelper.NormalizeRotation(input)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0, 90)]
    [Arguments(90, 180)]
    [Arguments(180, 270)]
    [Arguments(270, 0)]
    public async Task RotateClockwise_AdvancesByNinetyDegrees(int current, int expected)
    {
        await Assert.That(VideoTransformHelper.RotateClockwise(current)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0, 270)]
    [Arguments(270, 180)]
    [Arguments(180, 90)]
    [Arguments(90, 0)]
    public async Task RotateCounterClockwise_RetreatsByNinetyDegrees(int current, int expected)
    {
        await Assert.That(VideoTransformHelper.RotateCounterClockwise(current)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0, false, false, VideoTransform.Normal)]
    [Arguments(0, true, false, VideoTransform.FlipHorizontal)]
    [Arguments(0, false, true, VideoTransform.FlipVertical)]
    [Arguments(0, true, true, VideoTransform.Rotate180)]
    [Arguments(90, false, false, VideoTransform.Rotate90)]
    [Arguments(90, true, false, VideoTransform.Transpose)]
    [Arguments(90, false, true, VideoTransform.AntiTranspose)]
    [Arguments(90, true, true, VideoTransform.Rotate270)]
    [Arguments(180, false, false, VideoTransform.Rotate180)]
    [Arguments(180, true, false, VideoTransform.FlipVertical)]
    [Arguments(180, false, true, VideoTransform.FlipHorizontal)]
    [Arguments(180, true, true, VideoTransform.Normal)]
    [Arguments(270, false, false, VideoTransform.Rotate270)]
    [Arguments(270, true, false, VideoTransform.AntiTranspose)]
    [Arguments(270, false, true, VideoTransform.Transpose)]
    [Arguments(270, true, true, VideoTransform.Rotate90)]
    public async Task Compose_AllRotationAndFlipCombinations_ReturnsMatchingLibVlcOrientation(
        int rotation,
        bool flipHorizontal,
        bool flipVertical,
        VideoTransform expected)
    {
        VideoTransform actual = VideoTransformHelper.Compose(rotation, flipHorizontal, flipVertical);
        await Assert.That(actual).IsEqualTo(expected);
    }
}
