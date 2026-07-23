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
    /// Identifies the <see cref="Label"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(ChapterPickerControl),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the text description displayed on the chapter picker control.
    /// </summary>
    /// <value>The text description displayed on the chapter picker control.</value>
    public string Label
    {
        get { return (string)GetValue(LabelProperty); }
        set { SetValue(LabelProperty, value); }
    }

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
    /// Identifies the <see cref="ChapterCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ChapterCommandProperty = DependencyProperty.Register(
        nameof(ChapterCommand),
        typeof(ICommand),
        typeof(ChapterPickerControl),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command to invoke when the chapter item is pressed.
    /// </summary>
    /// <value>The command to invoke when the chapter item is pressed.</value>
    public ICommand ChapterCommand
    {
        get { return (ICommand)GetValue(ChapterCommandProperty); }
        set { SetValue(ChapterCommandProperty, value); }
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

    private void ChapterList_OnItemClick(object sender, ItemClickEventArgs e)
    {
        var cue = (ChapterCue)e.ClickedItem;
        ChapterCommand.Execute(cue);
    }
}
