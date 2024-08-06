using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;

namespace Screenbox.Controls.Interactions;
internal class NavigationViewAccessKeysBehavior : BehaviorBase<NavigationView>
{
    ///// <summary>
    ///// The dependency property for <see cref="BackButtonAccessKey"/>.
    ///// </summary>
    //public static readonly DependencyProperty BackButtonAccessKeyProperty = DependencyProperty.Register(
    //    nameof(BackButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(string.Empty));

    ///// <summary>
    ///// Get and set the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> back button access key.
    ///// </summary>
    //public string BackButtonAccessKey
    //{
    //    get => (string)GetValue(BackButtonAccessKeyProperty);
    //    set => SetValue(BackButtonAccessKeyProperty, value);
    //}

    ///// <summary>
    ///// The dependency property for <see cref="BackButtonKeyTipPlacementMode"/>.
    ///// </summary>
    //public static readonly DependencyProperty BackButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
    //    nameof(BackButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

    ///// <summary>
    ///// Get and set the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> back button access key placement.
    ///// </summary>
    //public KeyTipPlacementMode BackButtonKeyTipPlacementMode
    //{
    //    get => (KeyTipPlacementMode)GetValue(BackButtonKeyTipPlacementModeProperty);
    //    set => SetValue(BackButtonKeyTipPlacementModeProperty, value);
    //}

    ///// <summary>
    ///// The dependency property for <see cref="CloseButtonAccessKey"/>.
    ///// </summary>
    //public static readonly DependencyProperty CloseButtonAccessKeyProperty = DependencyProperty.Register(
    //    nameof(CloseButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(string.Empty));

    ///// <summary>
    ///// Get and set the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> close button access key.
    ///// </summary>
    //public string CloseButtonAccessKey
    //{
    //    get => (string)GetValue(CloseButtonAccessKeyProperty);
    //    set => SetValue(CloseButtonAccessKeyProperty, value);
    //}

    ///// <summary>
    ///// The dependency property for <see cref="CloseButtonKeyTipPlacementMode"/>.
    ///// </summary>
    //public static readonly DependencyProperty CloseButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
    //    nameof(CloseButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

    ///// <summary>
    ///// Get and set the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> close button access key placement.
    ///// </summary>
    //public KeyTipPlacementMode CloseButtonKeyTipPlacementMode
    //{
    //    get => (KeyTipPlacementMode)GetValue(CloseButtonKeyTipPlacementModeProperty);
    //    set => SetValue(CloseButtonKeyTipPlacementModeProperty, value);
    //}

    ///// <summary>
    ///// The dependency property for <see cref="TogglePaneButtonAccessKey"/>.
    ///// </summary>
    //public static readonly DependencyProperty TogglePaneButtonAccessKeyProperty = DependencyProperty.Register(
    //    nameof(TogglePaneButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(string.Empty));

    ///// <summary>
    ///// Get and set the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> pane button access key.
    ///// </summary>
    //public string TogglePaneButtonAccessKey
    //{
    //    get => (string)GetValue(TogglePaneButtonAccessKeyProperty);
    //    set => SetValue(TogglePaneButtonAccessKeyProperty, value);
    //}

    ///// <summary>
    ///// The dependency property for <see cref="TogglePaneButtonKeyTipPlacementMode"/>.
    ///// </summary>
    //public static readonly DependencyProperty TogglePaneButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
    //    nameof(TogglePaneButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(null));

    ///// <summary>
    ///// Get and set the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> pane button access key placement.
    ///// </summary>
    //public KeyTipPlacementMode TogglePaneButtonKeyTipPlacementMode
    //{
    //    get => (KeyTipPlacementMode)GetValue(TogglePaneButtonKeyTipPlacementModeProperty);
    //    set => SetValue(TogglePaneButtonKeyTipPlacementModeProperty, value);
    //}

    ///// <summary>
    ///// The dependency property for <see cref="PaneAutoSuggestButtonAccessKey"/>.
    ///// </summary>
    //public static readonly DependencyProperty PaneAutoSuggestButtonAccessKeyProperty = DependencyProperty.Register(
    //    nameof(PaneAutoSuggestButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(string.Empty));

    ///// <summary>
    ///// Get and set the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> auto suggest button access key.
    ///// </summary>
    //public string PaneAutoSuggestButtonAccessKey
    //{
    //    get => (string)GetValue(PaneAutoSuggestButtonAccessKeyProperty);
    //    set => SetValue(PaneAutoSuggestButtonAccessKeyProperty, value);
    //}

    ///// <summary>
    ///// The dependency property for <see cref="PaneAutoSuggestButtonKeyTipPlacementMode"/>.
    ///// </summary>
    //public static readonly DependencyProperty PaneAutoSuggestButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
    //    nameof(PaneAutoSuggestButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

    ///// <summary>
    ///// Get and set the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> auto suggest button access key placement.
    ///// </summary>
    //public KeyTipPlacementMode PaneAutoSuggestButtonKeyTipPlacementMode
    //{
    //    get => (KeyTipPlacementMode)GetValue(PaneAutoSuggestButtonKeyTipPlacementModeProperty);
    //    set => SetValue(PaneAutoSuggestButtonKeyTipPlacementModeProperty, value);
    //}

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();

        if (AssociatedObject.FindDescendant<Button>(bb => bb.Name == "NavigationViewBackButton") is { } navigationViewBackButton)
        {
            navigationViewBackButton.AccessKey = Strings.KeyboardResources.NavBackButtonKey;
            navigationViewBackButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;
            //navigationViewBackButton.AccessKey = BackButtonAccessKey;
            //navigationViewBackButton.KeyTipPlacementMode = BackButtonKeyTipPlacementMode;
            
        }

        if (AssociatedObject.FindDescendant<Button>(cb => cb.Name == "NavigationViewCloseButton") is { } navigationViewCloseButton)
        {
            navigationViewCloseButton.AccessKey = Strings.KeyboardResources.NavCloseButtonKey;
            navigationViewCloseButton.KeyTipPlacementMode = KeyTipPlacementMode.Bottom;
            //navigationViewCloseButton.AccessKey = CloseButtonAccessKey;
            //navigationViewCloseButton.KeyTipPlacementMode = CloseButtonKeyTipPlacementMode;
        }

        if (AssociatedObject.FindDescendant<Button>(pb => pb.Name == "TogglePaneButton") is { } togglePaneButton)
        {
            togglePaneButton.AccessKey = Strings.KeyboardResources.NavToggleMenuPaneButtonKey;
            togglePaneButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;
            //togglePaneButton.AccessKey = TogglePaneButtonAccessKey;
            //togglePaneButton.KeyTipPlacementMode = TogglePaneButtonKeyTipPlacementMode;
            
        }

        if (AssociatedObject.FindDescendant<Button>(sb => sb.Name == "PaneAutoSuggestButton") is { } paneAutoSuggestButton)
        {
            paneAutoSuggestButton.AccessKey = Strings.KeyboardResources.NavAutoSuggestButtonKey;
            paneAutoSuggestButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;
            //paneAutoSuggestButton.AccessKey = PaneAutoSuggestButtonAccessKey;
            //paneAutoSuggestButton.KeyTipPlacementMode = PaneAutoSuggestButtonKeyTipPlacementMode;

            // Disable button glyph text scaling
            // <see href="https://learn.microsoft.com/en-us/windows/apps/design/input/text-scaling#dont-scale-font-based-icons-or-symbols"/>
            paneAutoSuggestButton.IsTextScaleFactorEnabled = false;
        }
    }
}
