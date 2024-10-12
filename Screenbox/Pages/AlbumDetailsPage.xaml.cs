#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations.Expressions;
using Screenbox.Core;
using Screenbox.Core.ViewModels;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using EF = CommunityToolkit.WinUI.Animations.Expressions.ExpressionFunctions;
using NavigationViewDisplayMode = Windows.UI.Xaml.Controls.NavigationViewDisplayMode;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AlbumDetailsPage : Page
    {
        internal AlbumDetailsPageViewModel ViewModel => (AlbumDetailsPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private int ClampSize => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 64 : 96;

        private float BackgroundScaleFactor => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 0.75f : 0.625f;

        private float CoverScaleFactor => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 0.6f : 0.5f;

        private int ButtonPanelOffset => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 56 : 64;

        private float BackgroundVisualHeight => (float)(Header.ActualHeight * 2.5);

        private CompositionPropertySet? _props;
        private CompositionPropertySet? _scrollerPropertySet;
        private Compositor? _compositor;
        private SpriteVisual? _backgroundVisual;
        private ScrollViewer? _scrollViewer;

        public AlbumDetailsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<AlbumDetailsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.OnNavigatedTo(e.Parameter);
        }

        private async void AlbumDetailsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Retrieve the ScrollViewer that the GridView is using internally
            ScrollViewer scrollViewer = _scrollViewer = ItemList.FindDescendant<ScrollViewer>() ??
                                                        throw new Exception("Cannot find ScrollViewer in ListView");

            // Get the PropertySet that contains the scroll values from the ScrollViewer
            _scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            _compositor = _scrollerPropertySet.Compositor;

            // Create a PropertySet that has values to be referenced in the ExpressionAnimations below
            _props = _compositor.CreatePropertySet();
            _props.InsertScalar("progress", 0);
            _props.InsertScalar("clampSize", ClampSize);
            _props.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
            _props.InsertScalar("coverScaleFactor", CoverScaleFactor);
            _props.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
            _props.InsertScalar("headerPadding", 12);

            // Get references to our property sets for use with ExpressionNodes
            ManipulationPropertySetReferenceNode scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();

            CreateHeaderAnimation(_props, scrollingProperties.Translation.Y);

            if (ViewModel.Source.RelatedSongs.Count == 0) return;
            MediaViewModel firstSong = ViewModel.Source.RelatedSongs[0];
            if (firstSong.Thumbnail != null)
            {
                var thumbnailSource = await firstSong.GetThumbnailSourceAsync();
                if (thumbnailSource == null) return;
                CreateImageBackgroundGradientVisual(scrollingProperties.Translation.Y, thumbnailSource);
            }
            else
            {
                firstSong.PropertyChanged += OnPropertyChanged;
            }
        }

        private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_scrollerPropertySet == null) return;
            MediaViewModel media = (MediaViewModel)sender;
            if (e.PropertyName == nameof(MediaViewModel.Thumbnail) && media.Thumbnail != null)
            {
                media.PropertyChanged -= OnPropertyChanged;
                var thumbnailSource = await media.GetThumbnailSourceAsync();
                if (thumbnailSource == null) return;
                ManipulationPropertySetReferenceNode scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
                CreateImageBackgroundGradientVisual(scrollingProperties.Translation.Y, thumbnailSource);
            }
        }

        /// <summary>
        /// Create the animations that, during vertical scrolling, will gradually shrink the cover art, narrow the Y axis and fade in the background,
        /// include padding around the content, fade out the additional text blocks and fill the empty space left with the button panel.
        /// </summary>  
        /// <param name="propSet">A collection of properties values that are referenced to drive portions of the composition animations.</param>
        /// <param name="scrollVerticalOffset">A property set who has Translation.Y specified, the return from ElementCompositionPreview.GetScrollViewerManipulationPropertySet(...).</param>
        private void CreateHeaderAnimation(CompositionPropertySet propSet, ScalarNode scrollVerticalOffset)
        {
            PropertySetReferenceNode props = propSet.GetReference();
            ScalarNode progressNode = props.GetScalarProperty("progress");
            ScalarNode clampSizeNode = props.GetScalarProperty("clampSize");
            ScalarNode backgroundScaleFactorNode = props.GetScalarProperty("backgroundScaleFactor");
            ScalarNode coverScaleFactorNode = props.GetScalarProperty("coverScaleFactor");
            ScalarNode buttonPanelOffsetNode = props.GetScalarProperty("buttonPanelOffset");
            ScalarNode headerPaddingNode = props.GetScalarProperty("headerPadding");

            // Create and start an ExpressionAnimation to track scroll progress over the desired distance
            ExpressionNode progressAnimation = EF.Clamp(-scrollVerticalOffset / clampSizeNode, 0, 1);
            propSet.StartAnimation("progress", progressAnimation);

            // Get the backing visual for the background in the header so that its properties can be animated
            Visual backgroundVisual = ElementCompositionPreview.GetElementVisual(BackgroundAcrylic);

            // Create and start an ExpressionAnimation to scale and opacity fade in the backgound behind the header
            ExpressionNode backgroundScaleAnimation = EF.Lerp(1, backgroundScaleFactorNode, progressNode);
            ExpressionNode backgroundOpacityAnimation = progressNode;
            backgroundVisual.StartAnimation("Scale.Y", backgroundScaleAnimation);
            backgroundVisual.StartAnimation("Opacity", backgroundOpacityAnimation);

            // Get the backing visuals for the content container so that its properties can be animated
            Visual contentVisual = ElementCompositionPreview.GetElementVisual(ContentContainer);
            ElementCompositionPreview.SetIsTranslationEnabled(ContentContainer, true);

            // Create and start an ExpressionAnimation to move the content container with scroll position
            ExpressionNode contentTranslationAnimation = progressNode * headerPaddingNode;
            contentVisual.StartAnimation("Translation.Y", contentTranslationAnimation);

            // Get the backing visual for the cover art visual so that its properties can be animated
            Visual coverArtVisual = ElementCompositionPreview.GetElementVisual(CoverArt);
            ElementCompositionPreview.SetIsTranslationEnabled(CoverArt, true);

            // Create and start an ExpressionAnimation to scale and move the cover art with scroll position
            ExpressionNode coverArtScaleAnimation = EF.Lerp(1, coverScaleFactorNode, progressNode);
            ExpressionNode coverArtTranslationAnimation = progressNode * headerPaddingNode;
            coverArtVisual.StartAnimation("Scale.X", coverArtScaleAnimation);
            coverArtVisual.StartAnimation("Scale.Y", coverArtScaleAnimation);
            coverArtVisual.StartAnimation("Translation.X", coverArtTranslationAnimation);

            // Get the backing visual for the text panel so that its properties can be animated
            Visual textVisual = ElementCompositionPreview.GetElementVisual(TextPanel);
            ElementCompositionPreview.SetIsTranslationEnabled(TextPanel, true);

            // Create and start an ExpressionAnimation to move the text panel with scroll position
            ExpressionNode textTranslationAnimation = progressNode * (-clampSizeNode + headerPaddingNode);
            textVisual.StartAnimation("Translation.X", textTranslationAnimation);

            // Get backing visuals for the additional text blocks so that their properties can be animated
            Visual subtitleVisual = ElementCompositionPreview.GetElementVisual(SubtitleText);
            Visual captionVisual = ElementCompositionPreview.GetElementVisual(CaptionText);

            // Create an ExpressionAnimation that start opacity fade out animation with threshold for the additional text blocks
            ScalarNode fadeThreshold = ExpressionValues.Constant.CreateConstantScalar("fadeThreshold", 0.6f);
            ExpressionNode textFadeAnimation = 1 - EF.Conditional(progressNode < fadeThreshold, progressNode / fadeThreshold, 1);

            // Start opacity fade out animation on the additional text block visuals
            subtitleVisual.StartAnimation("Opacity", textFadeAnimation);
            textFadeAnimation.SetScalarParameter("fadeThreshold", 0.2f);
            captionVisual.StartAnimation("Opacity", textFadeAnimation);

            // Get the backing visual for the button panel so that its properties can be animated
            Visual buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);
            ElementCompositionPreview.SetIsTranslationEnabled(ButtonPanel, true);

            // Create and start an ExpressionAnimation to move the button panel with scroll position
            ExpressionNode buttonTranslationAnimation = progressNode * (-buttonPanelOffsetNode);
            buttonVisual.StartAnimation("Translation.Y", buttonTranslationAnimation);
        }

        private void CreateImageBackgroundGradientVisual(ScalarNode scrollVerticalOffset, IRandomAccessStream image)
        {
            if (_compositor == null) return;
            image.Seek(0);  // Manually seek the stream to start or image won't load
            LoadedImageSurface imageSurface = LoadedImageSurface.StartLoadFromStream(image);
            CompositionSurfaceBrush imageBrush = _compositor.CreateSurfaceBrush(imageSurface);
            imageBrush.HorizontalAlignmentRatio = 0.5f;
            imageBrush.VerticalAlignmentRatio = 0.25f;
            imageBrush.Stretch = CompositionStretch.UniformToFill;

            CompositionLinearGradientBrush gradientBrush = _compositor.CreateLinearGradientBrush();
            gradientBrush.EndPoint = new Vector2(0, 1);
            gradientBrush.MappingMode = CompositionMappingMode.Relative;
            gradientBrush.ColorStops.Add(_compositor.CreateColorGradientStop(0.4f, Colors.White));
            gradientBrush.ColorStops.Add(_compositor.CreateColorGradientStop(1, Colors.Transparent));

            CompositionMaskBrush maskBrush = _compositor.CreateMaskBrush();
            maskBrush.Source = imageBrush;
            maskBrush.Mask = gradientBrush;

            SpriteVisual visual = _backgroundVisual = _compositor.CreateSpriteVisual();
            visual.Size = new Vector2((float)BackgroundHost.ActualWidth, BackgroundVisualHeight);
            visual.Opacity = 0.15f;
            visual.Brush = maskBrush;

            visual.StartAnimation("Offset.Y", scrollVerticalOffset);
            imageBrush.StartAnimation("Offset.Y", -scrollVerticalOffset * 0.8f);

            ElementCompositionPreview.SetElementChildVisual(BackgroundHost, visual);
        }

        private void AlbumArt_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _props?.InsertScalar("clampSize", ClampSize);
            _props?.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
            _props?.InsertScalar("coverScaleFactor", CoverScaleFactor);
            _props?.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
        }

        private void BackgroundHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_backgroundVisual == null) return;
            _backgroundVisual.Size = new Vector2((float)e.NewSize.Width, BackgroundVisualHeight);
        }

        private Thickness GetScrollbarVerticalMargin(Thickness value)
        {
            double headerHeight = CoverArt.Height + Header.Margin.Bottom;
            return new Thickness(value.Left, value.Top - headerHeight, value.Right, value.Bottom);
        }

        private static string GetSubtext(uint? year, int songsCount, TimeSpan duration)
        {
            string songsCountText = Strings.Resources.SongsCount(songsCount);
            string runTime = Strings.Resources.RunTime(Humanizer.ToDuration(duration));
            StringBuilder builder = new();
            if (year != null)
            {
                builder.Append(year);
                builder.Append(" • ");
            }

            builder.AppendJoin(" • ", songsCountText, runTime);
            return builder.ToString();
        }
    }
}
