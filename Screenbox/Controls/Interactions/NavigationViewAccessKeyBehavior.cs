using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
//using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls.Interactions
{
    /// <summary>
    /// This behavior adds access keys to the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> built-in elements.
    /// </summary>
    internal class NavigationViewAccessKeyBehavior : BehaviorBase<Microsoft.UI.Xaml.Controls.NavigationView>
    {
        ///// <summary>
        ///// Identifies the <see cref="BackButtonAccessKey"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty BackButtonAccessKeyProperty = DependencyProperty.Register(
        //    nameof(BackButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeyBehavior), new PropertyMetadata(string.Empty));

        ///// <summary>
        ///// Gets or sets the access key of the back button.
        ///// </summary>
        //public string BackButtonAccessKey
        //{
        //    get => (string)GetValue(BackButtonAccessKeyProperty);
        //    set => SetValue(BackButtonAccessKeyProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="BackButtonKeyTipPlacementMode"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty BackButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
        //    nameof(BackButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeyBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

        ///// <summary>
        ///// Gets or sets where the key tip is placed in relation to the boundary of the back button.
        ///// </summary>
        //public KeyTipPlacementMode BackButtonKeyTipPlacementMode
        //{
        //    get => (KeyTipPlacementMode)GetValue(BackButtonKeyTipPlacementModeProperty);
        //    set => SetValue(BackButtonKeyTipPlacementModeProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="CloseButtonAccessKey"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty CloseButtonAccessKeyProperty = DependencyProperty.Register(
        //    nameof(CloseButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeyBehavior), new PropertyMetadata(string.Empty));

        ///// <summary>
        ///// Gets or sets the access key of the close button.
        ///// </summary>
        //public string CloseButtonAccessKey
        //{
        //    get => (string)GetValue(CloseButtonAccessKeyProperty);
        //    set => SetValue(CloseButtonAccessKeyProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="CloseButtonKeyTipPlacementMode"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty CloseButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
        //    nameof(CloseButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeyBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

        ///// <summary>
        ///// Gets or sets where the key tip is placed in relation to the boundary of the close button.
        ///// </summary>
        //public KeyTipPlacementMode CloseButtonKeyTipPlacementMode
        //{
        //    get => (KeyTipPlacementMode)GetValue(CloseButtonKeyTipPlacementModeProperty);
        //    set => SetValue(CloseButtonKeyTipPlacementModeProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="TogglePaneButtonAccessKey"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty TogglePaneButtonAccessKeyProperty = DependencyProperty.Register(
        //    nameof(TogglePaneButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeyBehavior), new PropertyMetadata(string.Empty));

        ///// <summary>
        ///// Gets or sets the access key of the menu toggle button.
        ///// </summary>
        //public string TogglePaneButtonAccessKey
        //{
        //    get => (string)GetValue(TogglePaneButtonAccessKeyProperty);
        //    set => SetValue(TogglePaneButtonAccessKeyProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="TogglePaneButtonKeyTipPlacementMode"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty TogglePaneButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
        //    nameof(TogglePaneButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeyBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

        ///// <summary>
        ///// Gets or sets where the key tip is placed in relation to the boundary of the menu toggle button.
        ///// </summary>
        //public KeyTipPlacementMode TogglePaneButtonKeyTipPlacementMode
        //{
        //    get => (KeyTipPlacementMode)GetValue(TogglePaneButtonKeyTipPlacementModeProperty);
        //    set => SetValue(TogglePaneButtonKeyTipPlacementModeProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="PaneAutoSuggestButtonAccessKey"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty PaneAutoSuggestButtonAccessKeyProperty = DependencyProperty.Register(
        //    nameof(PaneAutoSuggestButtonAccessKey), typeof(string), typeof(NavigationViewAccessKeyBehavior), new PropertyMetadata(string.Empty));

        ///// <summary>
        ///// Gets or sets the access key of the auto-suggest button.
        ///// </summary>
        //public string PaneAutoSuggestButtonAccessKey
        //{
        //    get => (string)GetValue(PaneAutoSuggestButtonAccessKeyProperty);
        //    set => SetValue(PaneAutoSuggestButtonAccessKeyProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="PaneAutoSuggestButtonKeyTipPlacementMode"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty PaneAutoSuggestButtonKeyTipPlacementModeProperty = DependencyProperty.Register(
        //    nameof(PaneAutoSuggestButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewAccessKeyBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

        ///// <summary>
        ///// Gets or sets where the key tip is placed in relation to the boundary of the auto-suggest button.
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
                navigationViewBackButton.AccessKey = Strings.KeyboardResources.NavigationBackButtonKey;
                navigationViewBackButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;
            }

            if (AssociatedObject.FindDescendant<Button>(cb => cb.Name == "NavigationViewCloseButton") is { } navigationViewCloseButton)
            {
                navigationViewCloseButton.AccessKey = Strings.KeyboardResources.NavigationCloseButtonKey;
                navigationViewCloseButton.KeyTipPlacementMode = KeyTipPlacementMode.Bottom;
            }

            if (AssociatedObject.FindDescendant<Button>(pb => pb.Name == "TogglePaneButton") is { } togglePaneButton)
            {
                togglePaneButton.AccessKey = Strings.KeyboardResources.NavigationTogglePaneButtonKey;
                togglePaneButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;
            }

            if (AssociatedObject.FindDescendant<Button>(sb => sb.Name == "PaneAutoSuggestButton") is { } paneAutoSuggestButton)
            {
                paneAutoSuggestButton.AccessKey = Strings.KeyboardResources.NavigationPaneAutoSuggestButtonKey;
                paneAutoSuggestButton.KeyTipPlacementMode = KeyTipPlacementMode.Right;

                // Disable button glyph text scaling
                // https://learn.microsoft.com/en-us/windows/apps/design/input/text-scaling#dont-scale-font-based-icons-or-symbols
                paneAutoSuggestButton.IsTextScaleFactorEnabled = false;
            }
        }
    }
}
