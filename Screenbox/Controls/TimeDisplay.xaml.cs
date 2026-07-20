#nullable enable

using Screenbox.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

/// <summary>
/// Represents a control that displays playback time information for a media item.
/// </summary>
[StyleTypedProperty(Property = nameof(TextBlockStyle), StyleTargetType = typeof(TextBlock))]
public sealed partial class TimeDisplay : UserControl
{
    /// <summary>
    /// Identifies the <see cref="Time"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
        nameof(Time),
        typeof(double),
        typeof(TimeDisplay),
        new PropertyMetadata(0d));

    /// <summary>
    /// Gets or sets the current elapsed playback time.
    /// </summary>
    /// <value>The current elapsed playback time in milliseconds. The default is <b>0</b>.</value>
    public double Time
    {
        get => (double)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Length"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
        nameof(Length),
        typeof(double),
        typeof(TimeDisplay),
        new PropertyMetadata(0d));

    /// <summary>
    /// Gets or sets the total playback length.
    /// </summary>
    /// <value>The total playback length in milliseconds. The default is <b>0</b>.</value>
    public double Length
    {
        get => (double)GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="TextBlockStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextBlockStyleProperty = DependencyProperty.Register(
        nameof(TextBlockStyle),
        typeof(Style),
        typeof(TimeDisplay),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the style that defines the look of the text elements.
    /// </summary>
    /// <value>The style that defines the look of the text elements. The default is <b>null</b>.</value>
    public Style TextBlockStyle
    {
        get => (Style)GetValue(TextBlockStyleProperty);
        set => SetValue(TextBlockStyleProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="IsRemainingTimeVisible"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsRemainingTimeVisibleProperty = DependencyProperty.Register(
        nameof(IsRemainingTimeVisible),
        typeof(bool),
        typeof(TimeDisplay),
        new PropertyMetadata(false, OnIsRemainingTimeVisiblePropertyChanged));

    /// <summary>
    /// Gets or sets a value that indicates whether the control displays remaining time
    /// instead of elapsed time.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the control displays remaining time;
    /// otherwise, <see langword="false"/>. The default is <b>false</b>.
    /// </value>
    public bool IsRemainingTimeVisible
    {
        get => (bool)GetValue(IsRemainingTimeVisibleProperty);
        set => SetValue(IsRemainingTimeVisibleProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeDisplay"/> class.
    /// </summary>
    public TimeDisplay()
    {
        this.InitializeComponent();
    }

    private void RootGrid_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        IsRemainingTimeVisible = !IsRemainingTimeVisible;
    }

    private static void OnIsRemainingTimeVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TimeDisplay)d;
        control.UpdateVisualState();
    }

    private string GetRemainingTime(double currentTime) => Humanizer.ToDuration(currentTime - Length);

    private void UpdateVisualState()
    {
        VisualStateManager.GoToState(this, IsRemainingTimeVisible ? nameof(RemainingTime) : nameof(ElapsedTime), true);
    }
}
