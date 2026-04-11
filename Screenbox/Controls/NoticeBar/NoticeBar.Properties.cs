#nullable enable

using Windows.UI.Xaml;

namespace Screenbox.Controls;

public sealed partial class NoticeBar
{
    /// <summary>
    /// Identifies the <see cref="IsOpen"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(NoticeBar),
        new PropertyMetadata(false, OnPropertyChanged));

    /// <summary>
    /// Gets or sets a value that indicates whether the <see cref="NoticeBar"/>
    /// is open.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="NoticeBar"/> is open;
    /// otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
    public bool IsOpen
    {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Title"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(NoticeBar),
        new PropertyMetadata(default(string), OnPropertyChanged));

    /// <summary>
    /// Gets or sets the title of the <see cref="NoticeBar"/>.
    /// </summary>
    /// <value>The title of the <see cref="NoticeBar"/>. The default
    /// is an empty <see langword="string"/>.</value>
    public string? Title
    {
        get { return (string?)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Message"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(NoticeBar),
        new PropertyMetadata(default(string), OnPropertyChanged));

    /// <summary>
    /// Gets or sets the message of the <see cref="NoticeBar"/>.
    /// </summary>
    /// <value>The message of the <see cref="NoticeBar"/>. The default
    /// is an empty <see langword="string"/>.</value>
    public string? Message
    {
        get { return (string?)GetValue(MessageProperty); }
        set { SetValue(MessageProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="ActionContent"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register(
        nameof(ActionContent),
        typeof(object),
        typeof(NoticeBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the action content of the <see cref="NoticeBar"/>.
    /// </summary>
    /// <value>The action content of the <see cref="NoticeBar"/>.
    /// The default is <see langword="null"/>.</value>
    public object ActionContent
    {
        get { return (object)GetValue(ActionContentProperty); }
        set { SetValue(ActionContentProperty, value); }
    }

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var noticeBar = (NoticeBar)sender;
        noticeBar.OnPropertyChanged(args);
    }
}
