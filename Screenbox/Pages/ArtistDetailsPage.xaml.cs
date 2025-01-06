#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations.Expressions;
using Screenbox.Core;
using Screenbox.Core.ViewModels;
using System;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;
using EF = CommunityToolkit.WinUI.Animations.Expressions.ExpressionFunctions;
using NavigationViewDisplayMode = Windows.UI.Xaml.Controls.NavigationViewDisplayMode;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArtistDetailsPage : Page
    {
        internal ArtistDetailsPageViewModel ViewModel => (ArtistDetailsPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private int ClampSize => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 64 : 96;

        private float BackgroundScaleFactor => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 0.75f : 0.625f;

        private float CoverScaleFactor => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 0.6f : 0.5f;

        private int ButtonPanelOffset => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 56 : 64;

        private CompositionPropertySet? _props;
        private CompositionPropertySet? _scrollerPropertySet;
        private Compositor? _compositor;
        private ScrollViewer? _scrollViewer;

        public ArtistDetailsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<ArtistDetailsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.OnNavigatedTo(e.Parameter);
        }

        private void ArtistDetailsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Set initial visual state
            VisualStateManager.GoToState(this, Common.NavigationViewDisplayMode.ToString(), false);

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

        private void ProfilePicture_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _props?.InsertScalar("clampSize", ClampSize);
            _props?.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
            _props?.InsertScalar("coverScaleFactor", CoverScaleFactor);
            _props?.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
        }

        private Thickness GetScrollbarVerticalMargin(Thickness value)
        {
            double headerHeight = CoverArt.Height + Header.Margin.Bottom;
            return new Thickness(value.Left, value.Top - headerHeight, value.Right, value.Bottom);
        }

        private static string GetSubtext(int albumsCount, int songsCount, TimeSpan duration)
        {
            string albumsCountText = Strings.Resources.AlbumsCount(albumsCount);
            string songsCountText = Strings.Resources.SongsCount(songsCount);
            string runTime = Strings.Resources.RunTime(Humanizer.ToDuration(duration));
            return $"{albumsCountText} • {songsCountText} • {runTime}";
        }
    }
}
