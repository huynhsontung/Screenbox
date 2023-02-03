using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
using Screenbox.ViewModels;
using Windows.UI.Composition;
using Microsoft.Extensions.DependencyInjection;
using EF = Microsoft.Toolkit.Uwp.UI.Animations.Expressions.ExpressionFunctions;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;

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

        private int ClampSize => Common.NavigationViewDisplayMode == NavigationViewDisplayMode.Minimal ? 58 : 98;

        private CompositionPropertySet? _props;
        private CompositionPropertySet? _scrollerPropertySet;
        private Compositor? _compositor;
        private ScrollViewer? _scrollViewer;

        public ArtistDetailsPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<ArtistDetailsPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is not ArtistViewModel artist)
                throw new ArgumentException("Navigation parameter is not an album");

            ViewModel.Source = artist;
        }

        private void ArtistDetailsPage_OnLoaded(object sender, RoutedEventArgs e)
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
            Visual albumArtVisual = ElementCompositionPreview.GetElementVisual(ProfilePicture);
            ElementCompositionPreview.SetIsTranslationEnabled(ProfilePicture, true);

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
            Visual artistNameVisual = ElementCompositionPreview.GetElementVisual(Subtext);

            // Create and start fade animation with threshold for subtexts
            ScalarNode fadeThreshold = ExpressionValues.Constant.CreateConstantScalar("fadeThreshold", 0.6f);
            ExpressionNode textFadeAnimation = 1 - EF.Conditional(progressNode < fadeThreshold, progressNode / fadeThreshold, 1);
            artistNameVisual.StartAnimation("Opacity", textFadeAnimation);

            // Get the backing visuals for the button containers so that their properties can be animated
            Visual buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);
            ElementCompositionPreview.SetIsTranslationEnabled(ButtonPanel, true);

            ExpressionNode buttonTranslationYAnimation = progressNode * (-clampSizeNode + bottomMarginNode);
            buttonVisual.StartAnimation("Translation.X", titleTranslationXAnimation);
            buttonVisual.StartAnimation("Translation.Y", buttonTranslationYAnimation);
        }

        private void ProfilePicture_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _props?.InsertScalar("clampSize", ClampSize);
            _props?.InsertScalar("bottomMargin", (float)ButtonPanel.Margin.Bottom);
            UpdateBackgroundAcrylicSize(_scrollViewer?.VerticalOffset ?? 0);
        }

        private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            UpdateBackgroundAcrylicSize(e.NextView.VerticalOffset);
        }

        private void UpdateBackgroundAcrylicSize(double scrollVerticalOffset)
        {
            // Animating visual size does not work. This is a work around.
            double progress = Math.Clamp(scrollVerticalOffset / ClampSize, 0, 1);
            double maxHeight = Header.Height;
            double minHeight = 102;
            BackgroundAcrylic.Height = maxHeight + (minHeight - maxHeight) * progress;

            // There is no way to detect over panning (i.e. scrollviewer translate but no view change)
            // Hide the background acrylic when the vertical srollbar is not visible where over panning is an issue
            BackgroundAcrylic.Visibility = _scrollViewer?.ComputedVerticalScrollBarVisibility ?? Visibility.Collapsed;
        }

        private Thickness GetScrollbarVerticalMargin(Thickness value)
        {
            double headerHeight = Header.Height + Header.Margin.Bottom;
            return new Thickness(value.Left, value.Top - headerHeight, value.Right, value.Bottom);
        }
    }
}
