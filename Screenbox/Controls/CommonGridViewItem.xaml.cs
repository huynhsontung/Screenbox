using Screenbox.Converters;
using Screenbox.Core.ViewModels;
using System;
using System.ComponentModel;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;
public sealed partial class CommonGridViewItem : UserControl
{
    public static readonly DependencyProperty ThumbnailHeightProperty = DependencyProperty.Register(
        nameof(ThumbnailHeight), typeof(double), typeof(CommonGridViewItem), new PropertyMetadata(double.NaN));

    public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
        nameof(PlayCommand), typeof(ICommand), typeof(CommonGridViewItem), new PropertyMetadata(default(ICommand)));

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
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue == null) return;
        if (args.NewValue is INotifyPropertyChanged observable)
        {
            observable.PropertyChanged += ObservableOnPropertyChanged;
        }

        string stateName;
        switch (args.NewValue)
        {
            case MediaViewModel media:
                stateName = "Media";
                PlaceholderIcon.Glyph = MediaGlyphConverter.Convert(media.MediaType);
                ThumbnailImage.Source = media.Thumbnail;
                CaptionText.Text = media.Caption ?? string.Empty;
                break;
            case AlbumViewModel album:
                stateName = "Album";
                PlaceholderIcon.Glyph = "\ue93c";
                ThumbnailImage.Source = album.AlbumArt;
                CaptionText.Text = album.ArtistName;
                break;
            default:
                throw new NotImplementedException();
        }

        VisualStateManager.GoToState(this, stateName, false);
    }

    private void ObservableOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "Thumbnail" when DataContext is MediaViewModel media:
                ThumbnailImage.Source = media.Thumbnail;
                break;
            case "Caption" when DataContext is MediaViewModel media:
                CaptionText.Text = media.Caption ?? string.Empty;
                break;
            case "AlbumArt" when DataContext is AlbumViewModel album:
                ThumbnailImage.Source = album.AlbumArt;
                break;
            case "ArtistName" when DataContext is AlbumViewModel album:
                CaptionText.Text = album.ArtistName;
                break;
        }
    }
}
