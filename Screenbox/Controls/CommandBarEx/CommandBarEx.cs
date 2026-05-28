#nullable enable

using System;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Screenbox.Controls;

/// <summary>
/// Represents an extended <see cref="CommandBar"/> control that provides a
/// convenient way to customize the "see more" button appearance using the
/// <see cref="MoreButtonStyle"/> property.
/// </summary>
/// <example>
/// This example demonstrates how to customize the "see more" button style
/// in a command bar.
/// <code lang="xml"><![CDATA[
/// <local:CommandBarEx MoreButtonStyle="{StaticResource AccentButtonStyle}">
///     <AppBarButton Icon="Add" Label="Add" />
///     <AppBarButton Icon="Edit" Label="Edit" />
///     <AppBarButton Icon="Share" Label="Share" />
/// </local:CommandBarEx>
/// ]]></code>
/// </example>
[StyleTypedProperty(Property = nameof(MoreButtonStyle), StyleTargetType = typeof(Button))]
public sealed class CommandBarEx : CommandBar
{
    private const string ExpandButtonName = "MoreButton";

    /// <summary>
    /// Identifies the <see cref="MoreButtonStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MoreButtonStyleProperty = DependencyProperty.Register(
        nameof(MoreButtonStyle),
        typeof(Style),
        typeof(CommandBarEx),
        new PropertyMetadata(null, (d, e) => ((CommandBarEx)d).OnPropertyChanged(e)));

    /// <summary>
    /// Gets or sets the Style that defines the look of the "see more" button.
    /// </summary>
    /// <value>The Style that defines the look of the "see more" button.
    /// The default is <see langword="null"/>.</value>
    public Style MoreButtonStyle
    {
        get { return (Style)GetValue(MoreButtonStyleProperty); }
        set { SetValue(MoreButtonStyleProperty, value); }
    }

    private Button? _expandButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBarEx"/> class.
    /// </summary>
    public CommandBarEx()
    {
        Unloaded += OnUnloaded;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild(ExpandButtonName) is Button expandButton)
        {
            _expandButton = expandButton;

            var visual = ElementCompositionPreview.GetElementVisual(expandButton);
            var compositor = visual.Compositor;
            ElementCompositionPreview.SetIsTranslationEnabled(expandButton, true);
            ElementCompositionPreview.SetImplicitShowAnimation(expandButton, CreateShowAnimationGroup(compositor));

            UpdateMoreButtonStyle();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _expandButton = null;
        Unloaded -= OnUnloaded;
    }

    private void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == MoreButtonStyleProperty)
        {
            UpdateMoreButtonStyle();
        }
    }

    private void UpdateMoreButtonStyle()
    {
        if (_expandButton is not null && MoreButtonStyle is not null)
        {
            _expandButton.Style = MoreButtonStyle;
        }
    }

    /// <summary>
    /// Creates an animation group that combines translation and opacity animations
    /// for the "see more" show implicit animation.
    /// </summary>
    /// <param name="compositor">The compositor used to create the animations.</param>
    /// <returns>A composition animation group containing the show animations.</returns>
    private static CompositionAnimationGroup CreateShowAnimationGroup(Compositor compositor)
    {
        var FastOutSlowInEasingFunction = compositor.CreateCubicBezierEasingFunction(new Vector2(0f, 0f), new Vector2(0f, 1f));
        var linearEasingFunction = compositor.CreateLinearEasingFunction();

        var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
        translationAnimation.InsertKeyFrame(0f, new Vector3(48f, 0f, 0f));
        translationAnimation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f), FastOutSlowInEasingFunction);
        translationAnimation.Duration = TimeSpan.FromMilliseconds(167);
        translationAnimation.Target = "Translation";

        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.InsertKeyFrame(0f, 0f);
        opacityAnimation.InsertKeyFrame(1f, 1f, linearEasingFunction);
        opacityAnimation.Duration = TimeSpan.FromMilliseconds(83);
        opacityAnimation.Target = "Opacity";

        var animationGroup = compositor.CreateAnimationGroup();
        animationGroup.Add(translationAnimation);
        animationGroup.Add(opacityAnimation);

        return animationGroup;
    }
}
