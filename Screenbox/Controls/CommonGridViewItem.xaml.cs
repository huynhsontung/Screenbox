using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;
public sealed partial class CommonGridViewItem : UserControl
{
    public static readonly DependencyProperty ThumbnailHeightProperty = DependencyProperty.Register(
        nameof(ThumbnailHeight), typeof(double), typeof(CommonGridViewItem), new PropertyMetadata(double.NaN));

    public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
        nameof(PlayCommand), typeof(ICommand), typeof(CommonGridViewItem), new PropertyMetadata(default(ICommand)));

    public static readonly DependencyProperty HorizontalTextAlignmentProperty = DependencyProperty.Register(
        nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(CommonGridViewItem), new PropertyMetadata(TextAlignment.Left));

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(CommonGridViewItem), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PlaceholderIconSourceProperty = DependencyProperty.Register(
        nameof(PlaceholderIconSource), typeof(IconSource), typeof(CommonGridViewItem), new PropertyMetadata(default(IconSource)));

    public static readonly DependencyProperty ThumbnailSourceProperty = DependencyProperty.Register(
        nameof(ThumbnailSource), typeof(ImageSource), typeof(CommonGridViewItem), new PropertyMetadata(default(ImageSource)));

    public ImageSource? ThumbnailSource
    {
        get => (ImageSource?)GetValue(ThumbnailSourceProperty);
        set => SetValue(ThumbnailSourceProperty, value);
    }

    public IconSource? PlaceholderIconSource
    {
        get => (IconSource?)GetValue(PlaceholderIconSourceProperty);
        set => SetValue(PlaceholderIconSourceProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public TextAlignment HorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
        set => SetValue(HorizontalTextAlignmentProperty, value);
    }

    public ICommand? PlayCommand
    {
        get => (ICommand?)GetValue(PlayCommandProperty);
        set => SetValue(PlayCommandProperty, value);
    }

    public double ThumbnailHeight
    {
        get => (double)GetValue(ThumbnailHeightProperty);
        set => SetValue(ThumbnailHeightProperty, value);
    }

    public CommonGridViewItem()
    {
        this.InitializeComponent();
        CornerRadius = new CornerRadius(4);
    }
}
