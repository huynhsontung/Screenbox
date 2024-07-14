using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;

namespace Screenbox.Controls.Interactions;
internal class NavigationViewAccessKeysBehavior : BehaviorBase<NavigationView>
{
    //public static readonly DependencyProperty BackButtonAccessKeyProperty = DependencyProperty.Register(
    //    nameof(BackButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(string.Empty));

    //public string BackButtonAccessKey
    //{
    //    get => (string)GetValue(BackButtonAccessKeyProperty);
    //    set => SetValue(BackButtonAccessKeyProperty, value);
    //}

    //public static readonly DependencyProperty BackButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
    //    nameof(BackButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

    //public KeyTipPlacementMode BackButtonKeyTipPlacementMode
    //{
    //    get => (KeyTipPlacementMode)GetValue(BackButtonKeyTipPlacementModeProperty);
    //    set => SetValue(BackButtonKeyTipPlacementModeProperty, value);
    //}

    //public static readonly DependencyProperty CloseButtonAccessKeyProperty = DependencyProperty.Register(
    //    nameof(CloseButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(string.Empty));

    //public string CloseButtonAccessKey
    //{
    //    get => (string)GetValue(CloseButtonAccessKeyProperty);
    //    set => SetValue(CloseButtonAccessKeyProperty, value);
    //}

    //public static readonly DependencyProperty CloseButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
    //    nameof(CloseButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

    //public KeyTipPlacementMode CloseButtonKeyTipPlacementMode
    //{
    //    get => (KeyTipPlacementMode)GetValue(CloseButtonKeyTipPlacementModeProperty);
    //    set => SetValue(CloseButtonKeyTipPlacementModeProperty, value);
    //}

    //public static readonly DependencyProperty TogglePaneButtonAccessKeyProperty = DependencyProperty.Register(
    //    nameof(TogglePaneButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(string.Empty));

    //public string TogglePaneButtonAccessKey
    //{
    //    get => (string)GetValue(TogglePaneButtonAccessKeyProperty);
    //    set => SetValue(TogglePaneButtonAccessKeyProperty, value);
    //}

    //public static readonly DependencyProperty TogglePaneButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
    //    nameof(TogglePaneButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(null));

    //public KeyTipPlacementMode TogglePaneButtonKeyTipPlacementMode
    //{
    //    get => (KeyTipPlacementMode)GetValue(TogglePaneButtonKeyTipPlacementModeProperty);
    //    set => SetValue(TogglePaneButtonKeyTipPlacementModeProperty, value);
    //}

    //public static readonly DependencyProperty PaneAutoSuggestButtonAccessKeyProperty = DependencyProperty.Register(
    //    nameof(PaneAutoSuggestButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(string.Empty));

    //public string PaneAutoSuggestButtonAccessKey
    //{
    //    get => (string)GetValue(PaneAutoSuggestButtonAccessKeyProperty);
    //    set => SetValue(PaneAutoSuggestButtonAccessKeyProperty, value);
    //}

    //public static readonly DependencyProperty PaneAutoSuggestButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
    //    nameof(PaneAutoSuggestButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeysBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

    //public KeyTipPlacementMode PaneAutoSuggestButtonKeyTipPlacementMode
    //{
    //    get => (KeyTipPlacementMode)GetValue(PaneAutoSuggestButtonKeyTipPlacementModeProperty);
    //    set => SetValue(PaneAutoSuggestButtonKeyTipPlacementModeProperty, value);
    //}

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();

        if (AssociatedObject.FindDescendant<Button>(btn => btn.Name == "NavigationViewBackButton") is { } navigationViewBackButton)
        {
            navigationViewBackButton.AccessKey = Strings.KeyboardResources.NavBackButtonKey;
            navigationViewBackButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;
            //navigationViewBackButton.AccessKey = BackButtonAccessKey;
            //navigationViewBackButton.KeyTipPlacementMode = BackButtonKeyTipPlacementMode;
            
        }

        if (AssociatedObject.FindDescendant<Button>(btn => btn.Name == "NavigationViewCloseButton") is { } navigationViewCloseButton)
        {
            navigationViewCloseButton.AccessKey = Strings.KeyboardResources.NavCloseButtonKey;
            navigationViewCloseButton.KeyTipPlacementMode = KeyTipPlacementMode.Bottom;
            //navigationViewCloseButton.AccessKey = CloseButtonAccessKey;
            //navigationViewCloseButton.KeyTipPlacementMode = CloseButtonKeyTipPlacementMode;
        }

        if (AssociatedObject.FindDescendant<Button>(btn => btn.Name == "TogglePaneButton") is { } togglePaneButton)
        {
            togglePaneButton.AccessKey = Strings.KeyboardResources.NavToggleMenuPaneButtonKey;
            togglePaneButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;
            //togglePaneButton.AccessKey = TogglePaneButtonAccessKey;
            //togglePaneButton.KeyTipPlacementMode = TogglePaneButtonKeyTipPlacementMode;
            
        }

        if (AssociatedObject.FindDescendant<Button>(btn => btn.Name == "PaneAutoSuggestButton") is { } paneAutoSuggestButton)
        {
            paneAutoSuggestButton.AccessKey = Strings.KeyboardResources.NavAutoSuggestButtonKey;
            paneAutoSuggestButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;
            paneAutoSuggestButton.IsTextScaleFactorEnabled = false;
            //paneAutoSuggestButton.AccessKey = PaneAutoSuggestButtonAccessKey;
            //paneAutoSuggestButton.KeyTipPlacementMode = PaneAutoSuggestButtonKeyTipPlacementMode;
        }
    }
}
