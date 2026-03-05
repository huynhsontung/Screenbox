#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Screenbox.Controls;

public sealed partial class TitleBar
{
    /// <summary>
    /// Identifies the <see cref="HeightMode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HeightModeProperty = DependencyProperty.Register(
        nameof(HeightMode),
        typeof(TitleBarHeightMode),
        typeof(TitleBar),
        new PropertyMetadata(TitleBarHeightMode.Standard, OnPropertyChanged));

    /// <summary>
    /// Gets or sets a value that indicates the preferred height of the title bar.
    /// </summary>
    /// <value>A value of the enumeration that specifies the title bar height.
    /// The default is <b>Standard</b>.</value>
    public TitleBarHeightMode HeightMode
    {
        get { return (TitleBarHeightMode)GetValue(HeightModeProperty); }
        set { SetValue(HeightModeProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Header"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(object),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the content for the title bar header.
    /// </summary>
    /// <value>The content of the title bar header. The default is <b>null</b>.</value>
    /// <remarks>
    /// You can set a data template for the Header by using the <see cref="HeaderTemplate"/> property.
    /// </remarks>
    public object Header
    {
        get { return (object)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="HeaderTemplate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
        nameof(HeaderTemplate),
        typeof(DataTemplate),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="DataTemplate"/> used to display the content
    /// of the title bar header.
    /// </summary>
    /// <value>The template that specifies the visualization of the header object.
    /// The default is <b>null</b>.</value>
    public DataTemplate HeaderTemplate
    {
        get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
        set { SetValue(HeaderTemplateProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="HeaderTransitions"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderTransitionsProperty = DependencyProperty.Register(
        nameof(HeaderTransitions),
        typeof(TransitionCollection),
        typeof(TitleBar),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of <see cref="Transition"/> style elements
    /// that apply to the header of a <see cref="TitleBar"/>.
    /// </summary>
    /// <value>The collection of <see cref="Transition"/> style elements that apply
    /// to the title bar header.</value>
    public TransitionCollection HeaderTransitions
    {
        get { return (TransitionCollection)GetValue(HeaderTransitionsProperty); }
        set { SetValue(HeaderTransitionsProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Icon"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(IconElement),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the graphic content of the title bar.
    /// </summary>
    /// <value>The graphic content of the title bar.</value>
    public IconElement Icon
    {
        get { return (IconElement)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Title"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the title text of the title bar.
    /// </summary>
    /// <value>The text title displayed on the title bar.</value>
    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Footer"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
        nameof(Footer),
        typeof(object),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the content for the title bar footer.
    /// </summary>
    /// <value>The content of the title bar footer. The default is <b>null</b>.</value>
    /// <remarks>
    /// You can set a data template for the Footer by using the <see cref="FooterTemplate"/> property.
    /// </remarks>
    public object Footer
    {
        get { return (object)GetValue(FooterProperty); }
        set { SetValue(FooterProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="FooterTemplate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FooterTemplateProperty = DependencyProperty.Register(
        nameof(FooterTemplate),
        typeof(DataTemplate),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="DataTemplate"/> used to display the content
    /// of the title bar footer.
    /// </summary>
    /// <value>The template that specifies the visualization of the footer object.
    /// The default is <b>null</b>.</value>
    public DataTemplate FooterTemplate
    {
        get { return (DataTemplate)GetValue(FooterTemplateProperty); }
        set { SetValue(FooterTemplateProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="FooterTransitions"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FooterTransitionsProperty = DependencyProperty.Register(
        nameof(FooterTransitions),
        typeof(TransitionCollection),
        typeof(TitleBar),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of <see cref="Transition"/> style elements
    /// that apply to the footer of a <see cref="TitleBar"/>.
    /// </summary>
    /// <value>The collection of <see cref="Transition"/> style elements that apply
    /// to the title bar footer.</value>
    public TransitionCollection FooterTransitions
    {
        get { return (TransitionCollection)GetValue(FooterTransitionsProperty); }
        set { SetValue(FooterTransitionsProperty, value); }
    }

    #region Caption Button Colors

    /// <summary>
    /// Identifies the <see cref="CaptionButtonBackgroundBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CaptionButtonBackgroundBrushProperty = DependencyProperty.Register(
        nameof(CaptionButtonBackgroundBrush),
        typeof(Brush),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> that fills the background of the
    /// system caption button.
    /// </summary>
    /// <value>The brush that fills the background of the system caption button.
    /// The default is <see langword="null"/>.</value>
    public Brush CaptionButtonBackgroundBrush
    {
        get { return (Brush)GetValue(CaptionButtonBackgroundBrushProperty); }
        set { SetValue(CaptionButtonBackgroundBrushProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CaptionButtonForegroundBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CaptionButtonForegroundBrushProperty = DependencyProperty.Register(
        nameof(CaptionButtonForegroundBrush),
        typeof(Brush),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> to apply to the content of the
    /// system caption button.
    /// </summary>
    /// <value>The brush used to apply to the content of the system caption button.
    /// The default is a <see langword="null"/> brush from a pure code perspective,
    /// but the default title bar style set this to Black (for <b>Light</b> theme)
    /// or White (for <b>Dark</b> theme).</value>
    public Brush CaptionButtonForegroundBrush
    {
        get { return (Brush)GetValue(CaptionButtonForegroundBrushProperty); }
        set { SetValue(CaptionButtonForegroundBrushProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CaptionButtonBackgroundPointerOverBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CaptionButtonBackgroundPointerOverBrushProperty = DependencyProperty.Register(
        nameof(CaptionButtonBackgroundPointerOverBrush),
        typeof(Brush),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> that fills the background of the
    /// system caption button when the pointer is over it.
    /// </summary>
    /// <value>The brush that fills the background of the system caption button
    /// while the pointer is over it. The default is <see langword="null"/>.</value>
    public Brush CaptionButtonBackgroundPointerOverBrush
    {
        get { return (Brush)GetValue(CaptionButtonBackgroundPointerOverBrushProperty); }
        set { SetValue(CaptionButtonBackgroundPointerOverBrushProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CaptionButtonForegroundPointerOverBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CaptionButtonForegroundPointerOverBrushProperty = DependencyProperty.Register(
        nameof(CaptionButtonForegroundPointerOverBrush),
        typeof(Brush),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> to apply to the content of the
    /// system caption button when the pointer is over it.
    /// </summary>
    /// <value>The brush used to apply to the content of the system caption button
    /// while the pointer is over it. The default is a <see langword="null"/> brush
    /// from a pure code perspective, but the default title bar style set this to Black
    /// (for <b>Light</b> theme) or White (for <b>Dark</b> theme).</value>
    public Brush CaptionButtonForegroundPointerOverBrush
    {
        get { return (Brush)GetValue(CaptionButtonForegroundPointerOverBrushProperty); }
        set { SetValue(CaptionButtonForegroundPointerOverBrushProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CaptionButtonBackgroundPressedBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CaptionButtonBackgroundPressedBrushProperty = DependencyProperty.Register(
      nameof(CaptionButtonBackgroundPressedBrush),
      typeof(Brush),
      typeof(TitleBar),
      new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> that fills the background of the
    /// system caption button when it's pressed.
    /// </summary>
    /// <value>The brush that fills the background of the system caption button
    /// when it's pressed. The default is <see langword="null"/>.</value>
    public Brush CaptionButtonBackgroundPressedBrush
    {
        get { return (Brush)GetValue(CaptionButtonBackgroundPressedBrushProperty); }
        set { SetValue(CaptionButtonBackgroundPressedBrushProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CaptionButtonForegroundPressedBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CaptionButtonForegroundPressedBrushProperty = DependencyProperty.Register(
        nameof(CaptionButtonForegroundPressedBrush),
        typeof(Brush),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> to apply to the content of the
    /// system caption button when it's pressed.
    /// </summary>
    /// <value>The brush used to apply to the content of the system caption button
    /// when it's pressed. The default is a <see langword="null"/> brush from a pure
    /// code perspective, but the default title bar style set this to Dim Gray
    /// (for <b>Light</b> theme) or Light Gray (for <b>Dark</b> theme).</value>
    public Brush CaptionButtonForegroundPressedBrush
    {
        get { return (Brush)GetValue(CaptionButtonForegroundPressedBrushProperty); }
        set { SetValue(CaptionButtonForegroundPressedBrushProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CaptionButtonBackgroundInactiveBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CaptionButtonBackgroundInactiveBrushProperty = DependencyProperty.Register(
      nameof(CaptionButtonBackgroundInactiveBrush),
      typeof(Brush),
      typeof(TitleBar),
      new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> that fills the background of the
    /// system caption button when it's inactive.
    /// </summary>
    /// <value>The brush that fills the background of the caption button
    /// when it's inactive. The default is <see langword="null"/>.</value>
    public Brush CaptionButtonBackgroundInactiveBrush
    {
        get { return (Brush)GetValue(CaptionButtonBackgroundInactiveBrushProperty); }
        set { SetValue(CaptionButtonBackgroundInactiveBrushProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CaptionButtonForegroundInactiveBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CaptionButtonForegroundInactiveBrushProperty = DependencyProperty.Register(
        nameof(CaptionButtonForegroundInactiveBrush),
        typeof(Brush),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> to apply to the content of the
    /// system caption button when it's inactive.
    /// </summary>
    /// <value>The brush used to apply to the content of the system caption button
    /// when it's inactive. The default is a <see langword="null"/> brush from a pure
    /// code perspective, but the default title bar style set this to Dark Gray
    /// (for <b>Light</b> theme) or Dim Gray (for <b>Dark</b> theme).</value>
    public Brush CaptionButtonForegroundInactiveBrush
    {
        get { return (Brush)GetValue(CaptionButtonForegroundInactiveBrushProperty); }
        set { SetValue(CaptionButtonForegroundInactiveBrushProperty, value); }
    }

    #endregion

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var owner = (TitleBar)sender;
        owner.OnPropertyChanged(args);
    }
}
