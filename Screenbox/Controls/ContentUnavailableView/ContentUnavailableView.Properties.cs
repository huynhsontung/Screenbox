#nullable enable

using Windows.UI.Xaml;

namespace Screenbox.Controls;

public sealed partial class ContentUnavailableView
{
    /// <summary>
    /// Identifies the <see cref="IsOpen"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(ContentUnavailableView),
        new PropertyMetadata(false, OnPropertyChanged));

    /// <summary>
    /// Gets or sets a value that indicates whether the <see cref="ContentUnavailableView"/>
    /// is open.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="ContentUnavailableView"/> is open;
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
        typeof(ContentUnavailableView),
        new PropertyMetadata(default(string), OnPropertyChanged));

    /// <summary>
    /// Gets or sets the title of the <see cref="ContentUnavailableView"/>.
    /// </summary>
    /// <value>The title of the <see cref="ContentUnavailableView"/>. The default
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
        typeof(ContentUnavailableView),
        new PropertyMetadata(default(string), OnPropertyChanged));

    /// <summary>
    /// Gets or sets the message of the <see cref="ContentUnavailableView"/>.
    /// </summary>
    /// <value>The message of the <see cref="ContentUnavailableView"/>. The default
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
        typeof(ContentUnavailableView),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the action content of the <see cref="ContentUnavailableView"/>.
    /// </summary>
    /// <value>The action content of the <see cref="ContentUnavailableView"/>.
    /// The default is <see langword="null"/>.</value>
    public object ActionContent
    {
        get { return (object)GetValue(ActionContentProperty); }
        set { SetValue(ActionContentProperty, value); }
    }

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var noticeBar = (ContentUnavailableView)sender;
        noticeBar.OnPropertyChanged(args);
    }
}
