using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Windows.System;
//using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls.Interactions
{
    /// <summary>
    /// This behavior adds access keys and keybord accelerators to the <see cref="Microsoft.UI.Xaml.Controls.NavigationView"/> built-in elements.
    /// </summary>
    internal class NavigationViewKeyboardBehavior : BehaviorBase<Microsoft.UI.Xaml.Controls.NavigationView>
    {
        ///// <summary>
        ///// Identifies the <see cref="BackButtonAccessKey"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty BackButtonAccessKeyProperty = DependencyProperty.Register(
        //    nameof(BackButtonAccessKey), typeof(string), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(string.Empty));

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
        //    nameof(BackButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

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
        //    nameof(CloseButtonAccessKey), typeof(string), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(string.Empty));

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
        //    nameof(CloseButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

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
        //    nameof(TogglePaneButtonAccessKey), typeof(string), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(string.Empty));

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
        //    nameof(TogglePaneButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

        ///// <summary>
        ///// Gets or sets where the key tip is placed in relation to the boundary of the menu toggle button.
        ///// </summary>
        //public KeyTipPlacementMode TogglePaneButtonKeyTipPlacementMode
        //{
        //    get => (KeyTipPlacementMode)GetValue(TogglePaneButtonKeyTipPlacementModeProperty);
        //    set => SetValue(TogglePaneButtonKeyTipPlacementModeProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="TogglePaneButtonKeyboardAcceleratorKey"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty TogglePaneButtonKeyboardAcceleratorKeyProperty = DependencyProperty.Register(
        //    nameof(TogglePaneButtonKeyboardAcceleratorKey), typeof(VirtualKey), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(null));

        ///// <summary>
        ///// Gets or sets the virtual key for the keyboard accelerator of the menu toggle button.
        ///// </summary>
        //public VirtualKey TogglePaneButtonKeyboardAcceleratorKey
        //{
        //    get => (VirtualKey)GetValue(TogglePaneButtonKeyboardAcceleratorKeyProperty);
        //    set => SetValue(TogglePaneButtonKeyboardAcceleratorKeyProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="TogglePaneButtonKeyboardAcceleratorModifiers"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty TogglePaneButtonKeyboardAcceleratorModifiersProperty = DependencyProperty.Register(
        //    nameof(TogglePaneButtonKeyboardAcceleratorModifiers), typeof(VirtualKeyModifiers), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(null));

        ///// <summary>
        ///// Gets or sets the virtual key to modify another keypress for the keyboard accelerator of the menu toggle button.
        ///// </summary>
        //public VirtualKeyModifiers TogglePaneButtonKeyboardAcceleratorModifiers
        //{
        //    get => (VirtualKeyModifiers)GetValue(TogglePaneButtonKeyboardAcceleratorModifiersProperty);
        //    set => SetValue(TogglePaneButtonKeyboardAcceleratorModifiersProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="PaneAutoSuggestButtonAccessKey"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty PaneAutoSuggestButtonAccessKeyProperty = DependencyProperty.Register(
        //    nameof(PaneAutoSuggestButtonAccessKey), typeof(string), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(string.Empty));

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
        //    nameof(PaneAutoSuggestButtonKeyTipPlacementMode), typeof(KeyTipPlacementMode), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(default(KeyTipPlacementMode)));

        ///// <summary>
        ///// Gets or sets where the key tip is placed in relation to the boundary of the auto-suggest button.
        ///// </summary>
        //public KeyTipPlacementMode PaneAutoSuggestButtonKeyTipPlacementMode
        //{
        //    get => (KeyTipPlacementMode)GetValue(PaneAutoSuggestButtonKeyTipPlacementModeProperty);
        //    set => SetValue(PaneAutoSuggestButtonKeyTipPlacementModeProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="PaneAutoSuggestButtonKeyboardAcceleratorKey"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty PaneAutoSuggestButtonKeyboardAcceleratorKeyProperty = DependencyProperty.Register(
        //    nameof(PaneAutoSuggestButtonKeyboardAcceleratorKey), typeof(VirtualKey), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(null));

        ///// <summary>
        ///// Gets or sets the virtual key for the keyboard accelerator of the auto-suggest button.
        ///// </summary>
        //public VirtualKey PaneAutoSuggestButtonKeyboardAcceleratorKey
        //{
        //    get => (VirtualKey)GetValue(PaneAutoSuggestButtonKeyboardAcceleratorKeyProperty);
        //    set => SetValue(PaneAutoSuggestButtonKeyboardAcceleratorKeyProperty, value);
        //}

        ///// <summary>
        ///// Identifies the <see cref="PaneAutoSuggestButtonKeyboardAcceleratorModifiers"/> dependency property.
        ///// </summary>
        //public static readonly DependencyProperty PaneAutoSuggestButtonKeyboardAcceleratorModifiersProperty = DependencyProperty.Register(
        //    nameof(PaneAutoSuggestButtonKeyboardAcceleratorModifiers), typeof(VirtualKeyModifiers), typeof(NavigationViewKeyboardBehavior), new PropertyMetadata(null));

        ///// <summary>
        ///// Gets or sets the virtual key to modify another keypress for the keyboard accelerator of the auto-suggest button.
        ///// </summary>
        //public VirtualKeyModifiers PaneAutoSuggestButtonKeyboardAcceleratorModifiers
        //{
        //    get => (VirtualKeyModifiers)GetValue(PaneAutoSuggestButtonKeyboardAcceleratorModifiersProperty);
        //    set => SetValue(PaneAutoSuggestButtonKeyboardAcceleratorModifiersProperty, value);
        //}

        private readonly string _backButtonAccessKey = Strings.KeyboardResources.NavigationBackButtonKey;
        private readonly string _closeButtonAccessKey = Strings.KeyboardResources.NavigationCloseButtonKey;
        private readonly string _togglePaneButtonAccessKey = Strings.KeyboardResources.NavigationTogglePaneButtonKey;
        private readonly string _paneAutoSuggestButtonAccessKey = Strings.KeyboardResources.NavigationPaneAutoSuggestButtonKey;

        private readonly KeyTipPlacementMode _rightAccessKeyPlacement = KeyTipPlacementMode.Right;
        private readonly KeyTipPlacementMode _bottomAccessKeyPlacement = KeyTipPlacementMode.Bottom;

        private readonly VirtualKey _togglePaneKeyboardAcceleratorKey = VirtualKey.E;
        private readonly VirtualKey _paneAutoSuggestKeyboardAcceleratorKey = VirtualKey.F;
        private readonly VirtualKeyModifiers _ctrlKeyboardAcceleratorModifier = VirtualKeyModifiers.Control;

        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();

            if (AssociatedObject.FindDescendant<Button>(bb => bb.Name == "NavigationViewBackButton") is { } navigationViewBackButton)
            {
                navigationViewBackButton.AccessKey = _backButtonAccessKey;
                navigationViewBackButton.KeyTipPlacementMode = _rightAccessKeyPlacement;
            }

            if (AssociatedObject.FindDescendant<Button>(cb => cb.Name == "NavigationViewCloseButton") is { } navigationViewCloseButton)
            {
                navigationViewCloseButton.AccessKey = _closeButtonAccessKey;
                navigationViewCloseButton.KeyTipPlacementMode = _bottomAccessKeyPlacement;
            }

            if (AssociatedObject.FindDescendant<Button>(pb => pb.Name == "TogglePaneButton") is { } togglePaneButton)
            {
                togglePaneButton.AccessKey = _togglePaneButtonAccessKey;
                togglePaneButton.KeyTipPlacementMode = _rightAccessKeyPlacement;

                togglePaneButton.KeyboardAccelerators.Add(new KeyboardAccelerator()
                {
                    Modifiers = _ctrlKeyboardAcceleratorModifier,
                    Key = _togglePaneKeyboardAcceleratorKey
                });
            }

            if (AssociatedObject.FindDescendant<Button>(sb => sb.Name == "PaneAutoSuggestButton") is { } paneAutoSuggestButton)
            {
                paneAutoSuggestButton.AccessKey = _paneAutoSuggestButtonAccessKey;
                paneAutoSuggestButton.KeyTipPlacementMode = _rightAccessKeyPlacement;

                paneAutoSuggestButton.KeyboardAccelerators.Add(new KeyboardAccelerator()
                {
                    Modifiers = _ctrlKeyboardAcceleratorModifier,
                    Key = _paneAutoSuggestKeyboardAcceleratorKey
                });

                // Disable button glyph text scaling
                // https://learn.microsoft.com/en-us/windows/apps/design/input/text-scaling#dont-scale-font-based-icons-or-symbols
                paneAutoSuggestButton.IsTextScaleFactorEnabled = false;
            }
        }
    }
}
