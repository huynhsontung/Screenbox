using System;
using System.Threading.Tasks;
using LibVLCSharp.Shared.Structures;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Tests.Playback;

public class PlaybackChapterListTests
{
    private static ChapterDescription CreateChapter(long timeOffset, long duration, string name)
    {
        return (ChapterDescription)Activator.CreateInstance(
            typeof(ChapterDescription),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: [timeOffset, duration, name],
            culture: null)!;
    }

    [Test]
    public async Task Load_NullChapters_ClearsCollection()
    {
        var chapterList = new PlaybackChapterList();
        chapterList.Load(null, TimeSpan.FromMinutes(10));

        await Assert.That(chapterList.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Load_ChapterStartingAfterNaturalDuration_IsFilteredOut()
    {
        // Replicates issue #992 where the single chapter starts at 01:54:39 but the video duration is ~56:28.
        var chapterList = new PlaybackChapterList();
        ChapterDescription[] chapters =
        [
            CreateChapter(
                (long)TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(54)).Add(TimeSpan.FromSeconds(39)).TotalMilliseconds,
                -1,
                "Credits")
        ];

        chapterList.Load(chapters, TimeSpan.FromMinutes(56).Add(TimeSpan.FromSeconds(28)));

        await Assert.That(chapterList.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Load_MultipleChaptersWithFaultyLastChapter_KeepsValidChaptersOnly()
    {
        var chapterList = new PlaybackChapterList();
        ChapterDescription[] chapters =
        [
            CreateChapter(0, (long)TimeSpan.FromMinutes(10).TotalMilliseconds, "Intro"),
            CreateChapter((long)TimeSpan.FromMinutes(10).TotalMilliseconds, (long)TimeSpan.FromMinutes(20).TotalMilliseconds, "Episode"),
            CreateChapter((long)TimeSpan.FromHours(2).TotalMilliseconds, -1, "Faulty Credits")
        ];

        chapterList.Load(chapters, TimeSpan.FromMinutes(45));

        await Assert.That(chapterList.Count).IsEqualTo(2);
        await Assert.That(chapterList[0].Title).IsEqualTo("Intro");
        await Assert.That(chapterList[1].Title).IsEqualTo("Episode");
    }

    [Test]
    public async Task Load_UnknownDuration_CalculatesDurationFromNextChapterAndDuration()
    {
        var chapterList = new PlaybackChapterList();
        ChapterDescription[] chapters =
        [
            CreateChapter(0, -1, "Chapter 1"),
            CreateChapter((long)TimeSpan.FromMinutes(15).TotalMilliseconds, -1, "Chapter 2")
        ];

        chapterList.Load(chapters, TimeSpan.FromMinutes(45));

        await Assert.That(chapterList.Count).IsEqualTo(2);
        await Assert.That(chapterList[0].Duration).IsEqualTo(TimeSpan.FromMinutes(15));
        await Assert.That(chapterList[1].Duration).IsEqualTo(TimeSpan.FromMinutes(30));
    }

    [Test]
    public async Task Load_NegativeTimeOffset_FiltersOut()
    {
        var chapterList = new PlaybackChapterList();
        ChapterDescription[] chapters =
        [
            CreateChapter(-5000, 10000, "Invalid"),
            CreateChapter(0, 10000, "Valid")
        ];

        chapterList.Load(chapters, TimeSpan.FromMinutes(10));

        await Assert.That(chapterList.Count).IsEqualTo(1);
        await Assert.That(chapterList[0].Title).IsEqualTo("Valid");
    }

    [Test]
    public async Task Load_UnsortedChapters_SortsChronologically()
    {
        var chapterList = new PlaybackChapterList();
        ChapterDescription[] chapters =
        [
            CreateChapter((long)TimeSpan.FromMinutes(20).TotalMilliseconds, 5000, "Part 2"),
            CreateChapter((long)TimeSpan.FromMinutes(5).TotalMilliseconds, 5000, "Part 1")
        ];

        chapterList.Load(chapters, TimeSpan.FromMinutes(30));

        await Assert.That(chapterList.Count).IsEqualTo(2);
        await Assert.That(chapterList[0].Title).IsEqualTo("Part 1");
        await Assert.That(chapterList[1].Title).IsEqualTo("Part 2");
    }
}
