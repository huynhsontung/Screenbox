using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Windows.Media.Core;

namespace Screenbox.Core.Tests.Helpers;

public class ChapterHelperTests
{
    [Test]
    public async Task Sanitize_NullChapters_ReturnsEmpty()
    {
        List<ChapterCue> result = ChapterHelper.Sanitize((IEnumerable<ChapterInfo>?)null, TimeSpan.FromMinutes(10));

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Sanitize_ChapterStartingAfterNaturalDuration_IsFilteredOut()
    {
        // Replicates issue #992 where a single chapter starts at 01:54:39 but the video duration is ~56:28.
        ChapterInfo[] chapters =
        [
            new(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(54)).Add(TimeSpan.FromSeconds(39)), TimeSpan.FromMilliseconds(-1), "Credits")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.FromMinutes(56).Add(TimeSpan.FromSeconds(28)));

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Sanitize_MultipleChaptersWithFaultyLastChapter_KeepsValidChaptersOnly()
    {
        ChapterInfo[] chapters =
        [
            new(TimeSpan.Zero, TimeSpan.FromMinutes(10), "Intro"),
            new(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), "Episode"),
            new(TimeSpan.FromHours(2), TimeSpan.FromMinutes(5), "Faulty Credits")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.FromMinutes(45));

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Title).IsEqualTo("Intro");
        await Assert.That(result[1].Title).IsEqualTo("Episode");
    }

    [Test]
    public async Task Sanitize_UnknownOrNegativeDuration_CalculatesDurationFromNextChapterAndNaturalDuration()
    {
        ChapterInfo[] chapters =
        [
            new(TimeSpan.Zero, TimeSpan.FromMilliseconds(-1), "Chapter 1"),
            new(TimeSpan.FromMinutes(15), TimeSpan.Zero, "Chapter 2")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.FromMinutes(45));

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Duration).IsEqualTo(TimeSpan.FromMinutes(15));
        await Assert.That(result[1].Duration).IsEqualTo(TimeSpan.FromMinutes(30));
    }

    [Test]
    public async Task Sanitize_NegativeStartTime_FiltersOut()
    {
        ChapterInfo[] chapters =
        [
            new(TimeSpan.FromSeconds(-5), TimeSpan.FromSeconds(10), "Invalid"),
            new(TimeSpan.Zero, TimeSpan.FromSeconds(10), "Valid")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.FromMinutes(10));

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Valid");
    }

    [Test]
    public async Task Sanitize_UnsortedChapters_SortsChronologically()
    {
        ChapterInfo[] chapters =
        [
            new(TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(5), "Part 2"),
            new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), "Part 1")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.FromMinutes(30));

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Title).IsEqualTo("Part 1");
        await Assert.That(result[1].Title).IsEqualTo("Part 2");
    }

    [Test]
    public async Task Sanitize_OverlappingDuration_ClampsToNextChapter()
    {
        ChapterInfo[] chapters =
        [
            new(TimeSpan.Zero, TimeSpan.FromMinutes(20), "Chapter 1"),
            new(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), "Chapter 2")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.FromMinutes(30));

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Duration).IsEqualTo(TimeSpan.FromMinutes(10));
        await Assert.That(result[1].Duration).IsEqualTo(TimeSpan.FromMinutes(10));
    }

    [Test]
    public async Task Sanitize_LastChapterExceedingDuration_ClampsToNaturalDuration()
    {
        ChapterInfo[] chapters =
        [
            new(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(30), "Ending")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.FromMinutes(30));

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Duration).IsEqualTo(TimeSpan.FromMinutes(5));
    }

    [Test]
    public async Task FromVlcDescriptions_NullChapters_ReturnsEmpty()
    {
        List<ChapterCue> result = ChapterHelper.FromVlcDescriptions(null, TimeSpan.FromMinutes(10));

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Sanitize_ChapterCuesOverload_SanitizesAndReturnsCues()
    {
        ChapterCue[] cues =
        [
            new() { Title = "Intro", StartTime = TimeSpan.Zero, Duration = TimeSpan.FromMinutes(5) },
            new() { Title = "OutOfBounds", StartTime = TimeSpan.FromHours(2), Duration = TimeSpan.FromMinutes(5) }
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(cues, TimeSpan.FromMinutes(30));

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Intro");
    }

    [Test]
    public async Task Sanitize_ZeroNaturalDuration_CalculatesDurationBetweenChaptersAndSetsLastToZero()
    {
        ChapterInfo[] chapters =
        [
            new(TimeSpan.Zero, TimeSpan.Zero, "Part 1"),
            new(TimeSpan.FromMinutes(10), TimeSpan.Zero, "Part 2")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.Zero);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Duration).IsEqualTo(TimeSpan.FromMinutes(10));
        await Assert.That(result[1].Duration).IsEqualTo(TimeSpan.Zero);
    }

    [Test]
    public async Task Sanitize_IdenticalStartTimes_CalculatesZeroDurationForFirst()
    {
        ChapterInfo[] chapters =
        [
            new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), "Duplicate 1"),
            new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), "Duplicate 2")
        ];

        List<ChapterCue> result = ChapterHelper.Sanitize(chapters, TimeSpan.FromMinutes(20));

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Duration).IsEqualTo(TimeSpan.Zero);
        await Assert.That(result[1].Duration).IsEqualTo(TimeSpan.FromMinutes(5));
    }
}
