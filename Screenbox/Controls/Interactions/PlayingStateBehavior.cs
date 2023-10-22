using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls.Interactions;
internal class PlayingStateBehavior : Behavior<Control>
{
    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
        nameof(IsPlaying), typeof(bool), typeof(PlayingStateBehavior), new PropertyMetadata(default(bool), OnIsPlayingChanged));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(PlayingStateBehavior), new PropertyMetadata(default(bool)));

    public bool IsActive
    {
        get { return (bool)GetValue(IsActiveProperty); }
        set { SetValue(IsActiveProperty, value); }
    }

    public bool IsPlaying
    {
        get { return (bool)GetValue(IsPlayingProperty); }
        set { SetValue(IsPlayingProperty, value); }
    }

    private bool _previouslyActive;

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        PlayingStateBehavior behavior = (PlayingStateBehavior)d;
        behavior.Update();
    }

    private void Update()
    {
        bool useTransitions = _previouslyActive && IsActive;
        _previouslyActive = IsActive;
        VisualStateManager.GoToState(AssociatedObject, IsPlaying ? "IsPlaying" : "IsNotPlaying", useTransitions);
    }
}
