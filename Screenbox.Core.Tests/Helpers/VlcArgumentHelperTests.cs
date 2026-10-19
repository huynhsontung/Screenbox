using System.Threading.Tasks;
using Screenbox.Core.Helpers;

namespace Screenbox.Core.Tests.Helpers;

public class VlcArgumentHelperTests
{
    [Test]
    public async Task SanitizeArguments_FiltersNonOptionTokensAndLoneDashes()
    {
        string raw = "invalid -v --network-caching=1000 -- trailing";
        string sanitized = VlcArgumentHelper.SanitizeArguments(raw);

        await Assert.That(sanitized).IsEqualTo("-v --network-caching=1000");
    }

    [Test]
    public async Task SanitizeArguments_WhenEmptyOrWhitespace_ReturnsEmpty()
    {
        await Assert.That(VlcArgumentHelper.SanitizeArguments("")).IsEqualTo(string.Empty);
        await Assert.That(VlcArgumentHelper.SanitizeArguments("   ")).IsEqualTo(string.Empty);
        await Assert.That(VlcArgumentHelper.SanitizeArguments(null!)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SanitizeArguments_TrimsExtraWhitespaceBetweenOptions()
    {
        string raw = "   --avcodec-hw=any     --fullscreen    ";
        string sanitized = VlcArgumentHelper.SanitizeArguments(raw);

        await Assert.That(sanitized).IsEqualTo("--avcodec-hw=any --fullscreen");
    }
}
