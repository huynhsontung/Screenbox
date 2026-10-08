using System.Linq;
using Screenbox.Core.Helpers;

namespace Screenbox.Core.Tests.Helpers;

public class GenreHelpersTests
{
    [Test]
    public async Task SplitGenres_WhenNullOrWhiteSpace_ShouldReturnEmpty()
    {
        await Assert.That(GenreHelpers.SplitGenres(null)).IsEmpty();
        await Assert.That(GenreHelpers.SplitGenres("")).IsEmpty();
        await Assert.That(GenreHelpers.SplitGenres("   ")).IsEmpty();
    }

    [Test]
    public async Task SplitGenres_WhenSingleGenre_ShouldReturnTrimmedGenre()
    {
        var result = GenreHelpers.SplitGenres("  Rock  ").ToList();
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo("Rock");
    }

    [Test]
    public async Task SplitGenres_WhenDelimitedByCommaOrSemicolon_ShouldReturnAllGenres()
    {
        var result = GenreHelpers.SplitGenres("Pop, Rock ; Alternative ; ; Ambient").ToList();
        await Assert.That(result.Count).IsEqualTo(4);
        await Assert.That(result).Contains("Pop");
        await Assert.That(result).Contains("Rock");
        await Assert.That(result).Contains("Alternative");
        await Assert.That(result).Contains("Ambient");
    }

    [Test]
    public async Task MatchesGenre_WhenSingleGenre_ShouldMatchCaseInsensitive()
    {
        await Assert.That(GenreHelpers.MatchesGenre("Rock", "rock")).IsTrue();
        await Assert.That(GenreHelpers.MatchesGenre("ROCK", "Rock")).IsTrue();
        await Assert.That(GenreHelpers.MatchesGenre("Pop", "Rock")).IsFalse();
        await Assert.That(GenreHelpers.MatchesGenre(null, "Rock")).IsFalse();
        await Assert.That(GenreHelpers.MatchesGenre("", "Rock")).IsFalse();
    }

    [Test]
    public async Task MatchesGenre_WhenMultiGenreDelimited_ShouldMatchAnyGenre()
    {
        await Assert.That(GenreHelpers.MatchesGenre("Rock, Pop", "Rock")).IsTrue();
        await Assert.That(GenreHelpers.MatchesGenre("Rock, Pop", "pop")).IsTrue();
        await Assert.That(GenreHelpers.MatchesGenre("Soundtrack; Classical", "Classical")).IsTrue();
        await Assert.That(GenreHelpers.MatchesGenre("Soundtrack; Classical", "Jazz")).IsFalse();
    }
}
