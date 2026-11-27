using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;

namespace Screenbox.Core.Tests.Services;

public class SubtitleServiceTests
{
    private static SubtitleService CreateService() =>
        new(new FilesService(NullLogger<FilesService>.Instance), NullLogger<SubtitleService>.Instance);

    [Test]
    public async Task MatchSubtitleFileNames_StrictSkeletonMatch_FindsMatchingSubtitlesOnly()
    {
        string[] candidates =
        [
            "Iron.Man.2008.en.srt",
            "Iron Man 2008 (1080p).fr.ass",
            "Iron.Man.2008.mp4",
            "Other.Movie.2008.srt"
        ];

        IReadOnlyList<string> matches = SubtitleService.MatchSubtitleFileNames("Iron.Man.2008.mkv", candidates);

        await Assert.That(matches.Count).IsEqualTo(2);
        await Assert.That(matches).Contains("Iron.Man.2008.en.srt");
        await Assert.That(matches).Contains("Iron Man 2008 (1080p).fr.ass");
    }

    [Test]
    public async Task MatchSubtitleFileNames_WhenStrictFails_FallsBackToPartialTokens()
    {
        string[] candidates =
        [
            "Iron.Man.en.srt",
            "Something.Else.srt"
        ];

        IReadOnlyList<string> matches = SubtitleService.MatchSubtitleFileNames("Iron.Man.2008.mkv", candidates);

        await Assert.That(matches.Count).IsEqualTo(1);
        await Assert.That(matches[0]).IsEqualTo("Iron.Man.en.srt");
    }

    [Test]
    public async Task FindPreferredSubtitleIndex_EmptyPreferencesOrEmptyTracks_ReturnsNegativeOne()
    {
        SubtitleService service = CreateService();
        SubtitleTrack[] tracks = [new("1", "English", "en")];

        await Assert.That(service.FindPreferredSubtitleIndex(Array.Empty<SubtitleTrack>(), "en")).IsEqualTo(-1);
        await Assert.That(service.FindPreferredSubtitleIndex(tracks, string.Empty)).IsEqualTo(-1);
    }

    [Test]
    public async Task FindPreferredSubtitleIndex_SingleTrackWithPreference_SelectsZero()
    {
        SubtitleService service = CreateService();
        SubtitleTrack[] tracks = [new("1", "Track 1", "ja")];

        int index = service.FindPreferredSubtitleIndex(tracks, "en,English");

        await Assert.That(index).IsEqualTo(0);
    }

    [Test]
    public async Task FindPreferredSubtitleIndex_MultipleTracks_MatchesHighestPriorityLanguage()
    {
        SubtitleService service = CreateService();
        SubtitleTrack[] tracks =
        [
            new("1", "French", "fr"),
            new("2", "Spanish", "es"),
            new("3", "English", "en")
        ];

        int index = service.FindPreferredSubtitleIndex(tracks, "es,en");

        await Assert.That(index).IsEqualTo(1);
    }
}
