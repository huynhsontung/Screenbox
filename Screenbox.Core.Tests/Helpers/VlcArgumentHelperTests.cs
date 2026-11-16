using System.Collections.Generic;
using System.Linq;
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

    [Test]
    public async Task SanitizeArguments_PreservesQuotesAndParseArgumentsRemovesThem()
    {
        string raw = "--freetype-font=\"Arial Rounded MT Bold\" --freetype-color=16753920";
        string sanitized = VlcArgumentHelper.SanitizeArguments(raw);

        await Assert.That(sanitized).IsEqualTo(raw);

        IReadOnlyList<string> parsed = VlcArgumentHelper.ParseArguments(sanitized);

        await Assert.That(parsed.Count).IsEqualTo(2);
        await Assert.That(parsed[0]).IsEqualTo("--freetype-font=Arial Rounded MT Bold");
        await Assert.That(parsed[1]).IsEqualTo("--freetype-color=16753920");
    }

    [Test]
    public async Task FormatArguments_ThenParseArguments_PreservesSpaceContainingOption()
    {
        string[] args = { "--freetype-font=Arial Rounded MT Bold", "--freetype-color=16753920" };

        string formatted = VlcArgumentHelper.FormatArguments(args);
        IReadOnlyList<string> parsed = VlcArgumentHelper.ParseArguments(formatted);

        await Assert.That(parsed.SequenceEqual(args)).IsTrue();
    }
}
