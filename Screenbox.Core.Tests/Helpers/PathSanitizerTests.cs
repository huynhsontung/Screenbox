using System.Threading.Tasks;
using Screenbox.Core.Helpers;

namespace Screenbox.Core.Tests.Helpers;

public class PathSanitizerTests
{
    [Test]
    public async Task GetExtensionOrType_StandardWindowsPath_ReturnsNormalizedExtension()
    {
        string path = @"C:\Users\JohnDoe\Music\song.MP3";
        string ext = PathSanitizer.GetExtensionOrType(path);

        await Assert.That(ext).IsEqualTo(".mp3");
    }

    [Test]
    public async Task GetExtensionOrType_UncPath_ReturnsExtension()
    {
        string path = @"\\server\share\videos\movie.mkv";
        string ext = PathSanitizer.GetExtensionOrType(path);

        await Assert.That(ext).IsEqualTo(".mkv");
    }

    [Test]
    public async Task GetExtensionOrType_FileUri_ReturnsExtension()
    {
        string path = "file:///C:/Users/SecretUser/video.mp4";
        string ext = PathSanitizer.GetExtensionOrType(path);

        await Assert.That(ext).IsEqualTo(".mp4");
    }

    [Test]
    public async Task GetExtensionOrType_HttpUri_ReturnsExtension()
    {
        string path = "https://example.com/media/stream.m3u8";
        string ext = PathSanitizer.GetExtensionOrType(path);

        await Assert.That(ext).IsEqualTo(".m3u8");
    }

    [Test]
    public async Task GetExtensionOrType_NoExtension_ReturnsEmpty()
    {
        string path = @"C:\Users\JohnDoe\folder";
        string ext = PathSanitizer.GetExtensionOrType(path);

        await Assert.That(ext).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GetExtensionOrType_NullOrWhitespace_ReturnsEmpty()
    {
        await Assert.That(PathSanitizer.GetExtensionOrType(null)).IsEqualTo(string.Empty);
        await Assert.That(PathSanitizer.GetExtensionOrType("   ")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SanitizeText_WindowsPathInSentence_ReplacesWithExtensionToken()
    {
        string input = @"Failed to open 'C:\Users\SecretUser\Music\secret_song.flac' with an external application.";
        string sanitized = PathSanitizer.SanitizeText(input);

        await Assert.That(sanitized).IsEqualTo("Failed to open '[file: .flac]' with an external application.");
    }

    [Test]
    public async Task SanitizeText_UncPathInSentence_ReplacesWithExtensionToken()
    {
        string input = @"Cannot find file '\\ServerName\ShareName\private\data.mkv'.";
        string sanitized = PathSanitizer.SanitizeText(input);

        await Assert.That(sanitized).IsEqualTo("Cannot find file '[file: .mkv]'.");
    }

    [Test]
    public async Task SanitizeText_FileWithoutExtension_ReplacesWithRedactedPath()
    {
        string input = @"Failed to read from C:\Users\Secret\folderName in directory.";
        string sanitized = PathSanitizer.SanitizeText(input);

        await Assert.That(sanitized).IsEqualTo("Failed to read from [redacted_path] in directory.");
    }

    [Test]
    public async Task SanitizeText_NoPath_ReturnsUnchanged()
    {
        string input = "Operation failed because network is unreachable.";
        string sanitized = PathSanitizer.SanitizeText(input);

        await Assert.That(sanitized).IsEqualTo(input);
    }

    [Test]
    public async Task SanitizeText_NullOrEmpty_ReturnsInput()
    {
        await Assert.That(PathSanitizer.SanitizeText(null)).IsEqualTo(string.Empty);
        await Assert.That(PathSanitizer.SanitizeText(string.Empty)).IsEqualTo(string.Empty);
    }
}
