#nullable enable

using System;
using System.Collections.Generic;
using Windows.Media.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

public sealed class ChapterTitleConverter : DependencyObject, IValueConverter
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

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return GetChapterTitle(value as ChapterCue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private string GetChapterTitle(ChapterCue? chapterCue)
    {
        if (chapterCue is null) return string.Empty;
        return !string.IsNullOrWhiteSpace(chapterCue.Title) || Chapters is null
            ? chapterCue.Title.TrimStart()
            : Screenbox.Strings.Resources.ChapterName(Chapters.IndexOf(chapterCue) + 1);
    }
}
