using System;
using System.Collections.Generic;
using Windows.Media.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

public sealed partial class ChapterTitleConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty ChaptersProperty = DependencyProperty.Register(
        nameof(Chapters),
        typeof(IList<ChapterCue>),
        typeof(ChapterTitleConverter),
        new PropertyMetadata(null));

    public IList<ChapterCue>? Chapters
    {
        get => (IList<ChapterCue>?)GetValue(ChaptersProperty);
        set => SetValue(ChaptersProperty, value);
    }

    /// <summary>
    /// Gets a display-ready title for the specified chapter cue.
    /// </summary>
    /// <param name="chapterCue">The chapter cue for which to get the title.</param>
    /// <param name="chapters">The list of chapter cues.</param>
    /// <returns>
    /// The chapter title with leading whitespace removed if available; otherwise,
    /// a fallback title derived from the chapter index.
    /// </returns>
    public static string GetChapterTitle(ChapterCue? chapterCue, IList<ChapterCue>? chapters)
    {
        if (chapterCue is null) return string.Empty;
        return !string.IsNullOrWhiteSpace(chapterCue.Title) || chapters is null
            ? chapterCue.Title.TrimStart()
            : Strings.Resources.ChapterName(chapters.IndexOf(chapterCue) + 1);
    }

    [DynamicWindowsRuntimeCast(typeof(ChapterCue))]
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is not ChapterCue chapter
            ? DependencyProperty.UnsetValue
            : GetChapterTitle(chapter, Chapters);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
