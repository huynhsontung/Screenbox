#nullable enable

using System;
using System.Numerics;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
using Screenbox.ViewModels;
using EF = Microsoft.Toolkit.Uwp.UI.Animations.Expressions.ExpressionFunctions;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;

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

        private int ClampSize => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 58 : 98;

        private float BackgroundVisualHeight => (float)(Header.Height + 200);

        private CompositionPropertySet? _props;
        private CompositionPropertySet? _scrollerPropertySet;
        private Compositor? _compositor;
        private SpriteVisual? _backgroundVisual;
        private ScrollViewer? _scrollViewer;

        public AlbumDetailsPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<AlbumDetailsPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is not AlbumViewModel album)
                throw new ArgumentException("Navigation parameter is not an album");

            ViewModel.Source = album;
        }

        private void AlbumDetailsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Retrieve the ScrollViewer that the GridView is using internally
            ScrollViewer scrollViewer = _scrollViewer = ItemList.FindDescendant<ScrollViewer>() ??
                                                        throw new Exception("Cannot find ScrollViewer in ListView");
            scrollViewer.ViewChanging += ScrollViewerOnViewChanging;

            // Get the PropertySet that contains the scroll values from the ScrollViewer
            _scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            _compositor = _scrollerPropertySet.Compositor;

            // Create a PropertySet that has values to be referenced in the ExpressionAnimations below
            _props = _compositor.CreatePropertySet();
            _props.InsertScalar("progress", 0);
            _props.InsertScalar("clampSize", ClampSize);
            _props.InsertScalar("bottomMargin", (float)ButtonPanel.Margin.Bottom);

            // Get references to our property sets for use with ExpressionNodes
            ManipulationPropertySetReferenceNode scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
            
            CreateHeaderAnimation(scrollingProperties.Translation.Y);
            CreateImageBackgroundGradientVisual(scrollingProperties.Translation.Y);
        }

        private void CreateHeaderAnimation(ScalarNode scrollVerticalOffset)
        {
            PropertySetReferenceNode props = _props.GetReference();
            ScalarNode progressNode = props.GetScalarProperty("progress");
            ScalarNode clampSizeNode = props.GetScalarProperty("clampSize");
            ScalarNode bottomMarginNode = props.GetScalarProperty("bottomMargin");

            // Create and start an ExpressionAnimation to track scroll progress over the desired distance
            ExpressionNode progressAnimation = EF.Clamp(-scrollVerticalOffset / clampSizeNode, 0, 1);
            _props.StartAnimation("progress", progressAnimation);

            // Get the backing visual for the photo in the header so that its properties can be animated
            Visual photoVisual = ElementCompositionPreview.GetElementVisual(BackgroundAcrylic);

            // Create and start an ExpressionAnimation to opacity fade out the image behind the header
            ExpressionNode imageOpacityAnimation = progressNode;
            photoVisual.StartAnimation("Opacity", imageOpacityAnimation);

            // Get the backing visual for the profile picture visual so that its properties can be animated
            Visual albumArtVisual = ElementCompositionPreview.GetElementVisual(AlbumArt);
            ElementCompositionPreview.SetIsTranslationEnabled(AlbumArt, true);

            // Create and start an ExpressionAnimation to scale the profile image with scroll position
            ScalarNode scaleFactorNode = 1 - clampSizeNode / albumArtVisual.GetReference().Size.X;
            ExpressionNode scaleAnimation = EF.Lerp(1, scaleFactorNode, progressNode);
            ExpressionNode albumArtTranslateAnimation = progressNode * 10;
            albumArtVisual.StartAnimation("Scale.X", scaleAnimation);
            albumArtVisual.StartAnimation("Scale.Y", scaleAnimation);
            albumArtVisual.StartAnimation("Translation.X", albumArtTranslateAnimation);

            // Get the backing visual for the title panel visual so that its properties can be animated
            Visual titleVisual = ElementCompositionPreview.GetElementVisual(TitlePanel);
            ElementCompositionPreview.SetIsTranslationEnabled(TitlePanel, true);

            // Create and start and ExpressionAnimation to translate title text
            ExpressionNode titleTranslationXAnimation = progressNode * (-clampSizeNode + 5);
            titleVisual.StartAnimation("Translation.X", titleTranslationXAnimation);

            // Get the backing visual for artist name and subtext
            Visual artistNameVisual = ElementCompositionPreview.GetElementVisual(ArtistNameText);
            Visual subtextVisual = ElementCompositionPreview.GetElementVisual(Subtext);

            // Create and start fade animation with threshold for subtexts
            ScalarNode fadeThreshold = ExpressionValues.Constant.CreateConstantScalar("fadeThreshold", 0.6f);
            ExpressionNode textFadeAnimation = 1 - EF.Conditional(progressNode < fadeThreshold, progressNode / fadeThreshold, 1);
            artistNameVisual.StartAnimation("Opacity", textFadeAnimation);
            textFadeAnimation.SetScalarParameter("fadeThreshold", 0.2f);
            subtextVisual.StartAnimation("Opacity", textFadeAnimation);
            

            // Get the backing visuals for the button containers so that their properties can be animated
            Visual buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);
            ElementCompositionPreview.SetIsTranslationEnabled(ButtonPanel, true);

            ExpressionNode buttonTranslationYAnimation = progressNode * (-clampSizeNode + bottomMarginNode);
            buttonVisual.StartAnimation("Translation.X", titleTranslationXAnimation);
            buttonVisual.StartAnimation("Translation.Y", buttonTranslationYAnimation);
        }

        private void CreateImageBackgroundGradientVisual(ScalarNode scrollVerticalOffset)
        {
            StorageItemThumbnail? image = ViewModel.Source.RelatedSongs[0].ThumbnailSource;
            if (image == null || _compositor == null) return;
            image.Seek(0);  // Manually seek the stream to start or image won't load
            LoadedImageSurface imageSurface = LoadedImageSurface.StartLoadFromStream(image);
            CompositionSurfaceBrush imageBrush = _compositor.CreateSurfaceBrush(imageSurface);
            imageBrush.HorizontalAlignmentRatio = 0.5f;
            imageBrush.VerticalAlignmentRatio = 0.5f;
            imageBrush.Stretch = CompositionStretch.UniformToFill;

            CompositionLinearGradientBrush gradientBrush = _compositor.CreateLinearGradientBrush();
            gradientBrush.EndPoint = new Vector2(0, 1);
            gradientBrush.MappingMode = CompositionMappingMode.Relative;
            gradientBrush.ColorStops.Add(_compositor.CreateColorGradientStop(0.6f, Colors.White));
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

        private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            UpdateBackgroundAcrylicSize(e.NextView.VerticalOffset);
        }

        private void AlbumArt_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _props?.InsertScalar("clampSize", ClampSize);
            _props?.InsertScalar("bottomMargin", (float)ButtonPanel.Margin.Bottom);
            UpdateBackgroundAcrylicSize(_scrollViewer?.VerticalOffset ?? 0);
        }

        private void UpdateBackgroundAcrylicSize(double scrollVerticalOffset)
        {
            // Animating visual size does not work. This is a work around.
            double progress = Math.Clamp(scrollVerticalOffset / ClampSize, 0, 1);
            double maxHeight = Header.Height;
            double minHeight = 102;
            BackgroundAcrylic.Height = maxHeight + (minHeight - maxHeight) * progress;
        }

        private void BackgroundHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_backgroundVisual == null) return;
            _backgroundVisual.Size = new Vector2((float)e.NewSize.Width, BackgroundVisualHeight);
        }

        private Thickness GetScrollbarVerticalMargin(Thickness value)
        {
            double headerHeight = Header.Height + Header.Margin.Bottom;
            return new Thickness(value.Left, value.Top - headerHeight, value.Right, value.Bottom);
        }
    }
}
