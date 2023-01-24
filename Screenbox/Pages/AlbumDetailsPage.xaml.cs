using System;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
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
        private CompositionPropertySet? _props;
        private CompositionPropertySet? _scrollerPropertySet;
        private Compositor? _compositor;
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

            // Get references to our property sets for use with ExpressionNodes
            ManipulationPropertySetReferenceNode scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
            PropertySetReferenceNode props = _props.GetReference();
            ScalarNode progressNode = props.GetScalarProperty("progress");
            ScalarNode clampSizeNode = props.GetScalarProperty("clampSize");

            // Create and start an ExpressionAnimation to track scroll progress over the desired distance
            ExpressionNode progressAnimation = EF.Clamp(-scrollingProperties.Translation.Y / clampSizeNode, 0, 1);
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
            ExpressionNode titleTranslationXAnimation = progressNode * -94;
            titleVisual.StartAnimation("Translation.X", titleTranslationXAnimation);

            Visual artistNameVisual = ElementCompositionPreview.GetElementVisual(ArtistNameText);
            ExpressionNode artistNameOpacityAnimation = 1 - EF.Conditional(progressNode < 0.6f, progressNode / 0.6f, 1);
            artistNameVisual.StartAnimation("Opacity", artistNameOpacityAnimation);

            // Get the backing visuals for the button containers so that their properties can be animated
            Visual buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);
            ElementCompositionPreview.SetIsTranslationEnabled(ButtonPanel, true);

            // // When the header stops scrolling it is 150 pixels offscreen.  We want the text header to end up with 50 pixels of its content
            // // offscreen which means it needs to go from offset 0 to 100 as we traverse through the scrollable region
            // ExpressionNode contentOffsetAnimation = progressNode * 100;
            // textVisual.StartAnimation("Offset.Y", contentOffsetAnimation);
            
            ExpressionNode buttonTranslationYAnimation = progressNode * -58;
            buttonVisual.StartAnimation("Translation.X", titleTranslationXAnimation);
            buttonVisual.StartAnimation("Translation.Y", buttonTranslationYAnimation);
        }

        private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            UpdateBackgroundAcrylicSize(e.NextView.VerticalOffset);
        }

        private void AlbumArt_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _props?.InsertScalar("clampSize", ClampSize);
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
    }
}
