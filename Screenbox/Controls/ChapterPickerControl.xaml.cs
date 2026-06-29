#nullable enable

using System.Collections.Generic;
using System.Windows.Input;
using Windows.Media.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

public sealed partial class ChapterPickerControl : UserControl
{
    /// <summary>
    /// Identifies the <see cref="SelectedChapter"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedChapterProperty = DependencyProperty.Register(
        nameof(SelectedChapter),
        typeof(ChapterCue),
        typeof(ChapterPickerControl),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the selected chapter.
    /// </summary>
    /// <value>The selected chapter. The default is <see langword="null"/>.</value>
    public ChapterCue SelectedChapter
    {
        get { return (ChapterCue)GetValue(SelectedChapterProperty); }
        set { SetValue(SelectedChapterProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="ChapterSelectedCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ChapterSelectedCommandProperty = DependencyProperty.Register(
        nameof(ChapterSelectedCommand),
        typeof(ICommand),
        typeof(ChapterPickerControl),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command to invoke when the chapter item is pressed.
    /// </summary>
    /// <value>The command to invoke when the chapter item is pressed.</value>
    public ICommand ChapterSelectedCommand
    {
        get { return (ICommand)GetValue(ChapterSelectedCommandProperty); }
        set { SetValue(ChapterSelectedCommandProperty, value); }
    }

    /// <summary>
    /// Gets or sets the chapters.
    /// </summary>
    /// <value>A collection of <see cref="ChapterCue"/> classes.</value>
    public IReadOnlyList<ChapterCue>? ChaptersSource { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChapterPickerControl"/> class.
    /// </summary>
    public ChapterPickerControl()
    {
        this.InitializeComponent();
    }

    private void ChapterList_OnLoaded(object sender, RoutedEventArgs e)
    {
        ChapterList.ScrollIntoView(SelectedChapter);
    }
}
