using CommunityToolkit.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;

namespace Screenbox.Extensions;

/// <summary>
/// Provides attached dependency properties for the buttons of the <see cref="SplitButton"/> control.
/// </summary>
public static class SplitButtonExtensions
{
    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding a <see cref="ToolTip"/> to the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    public static readonly DependencyProperty PrimaryButtonToolTipProperty = DependencyProperty.RegisterAttached(
       "PrimaryButtonToolTip", typeof(object), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnPrimaryButtonToolTipPropertyChanged));

    /// <summary>
    /// Gets the <see cref="ToolTip"/> for the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    /// <returns>The <see cref="object"/> associated with the <see cref="SplitButton"/> primary <see cref="Button"/> tooltip.</returns>
    public static object GetPrimaryButtonToolTip(SplitButton element)
    {
        return (object)element.GetValue(PrimaryButtonToolTipProperty);
    }

    /// <summary>
    /// Sets the <see cref="ToolTip"/> to the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    public static void SetPrimaryButtonToolTip(SplitButton element, object value)
    {
        element.SetValue(PrimaryButtonToolTipProperty, value);
    }

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="ToolTipService.PlacementProperty"/> to the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty PrimaryButtonToolTipPlacementProperty = DependencyProperty.RegisterAttached(
    //    "PrimaryButtonToolTipPlacement", typeof(PlacementMode), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnPrimaryButtonToolTipPlacementPropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="ToolTipService.PlacementProperty"/> for the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The <see cref="PlacementMode"/> <see cref="enum"/> associated with the <see cref="SplitButton"/> primary <see cref="Button"/> tooltip.</returns>
    //public static PlacementMode GetPrimaryButtonToolTipPlacement(SplitButton element)
    //{
    //    return (PlacementMode)element.GetValue(PrimaryButtonToolTipPlacementProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="ToolTipService.PlacementProperty"/> to the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetPrimaryButtonToolTipPlacement(SplitButton element, PlacementMode value)
    //{
    //    element.SetValue(PrimaryButtonToolTipPlacementProperty, value);
    //}

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="ToolTipService.PlacementTargetProperty"/> to the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty PrimaryButtonToolTipPlacementTargetProperty = DependencyProperty.RegisterAttached(
    //    "PrimaryButtonToolTipPlacementTarget", typeof(UIElement), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnPrimaryButtonToolTipPlacementTargetPropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="ToolTipService.PlacementTargetProperty"/> for the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The target <see cref="UIElement"/> associated with the <see cref="SplitButton"/> primary <see cref="Button"/> tooltip placement.</returns>
    //public static UIElement GetPrimaryButtonToolTipPlacementTarget(SplitButton element)
    //{
    //    return (UIElement)element.GetValue(PrimaryButtonToolTipPlacementTargetProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="ToolTipService.PlacementTargetProperty"/> to the primary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetPrimaryButtonToolTipPlacementTarget(SplitButton element, UIElement value)
    //{
    //    element.SetValue(PrimaryButtonToolTipPlacementTargetProperty, value);
    //}

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding a <see cref="UIElement.AccessKey"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    public static readonly DependencyProperty SecondaryButtonAccessKeyProperty = DependencyProperty.RegisterAttached(
        "SecondaryButtonAccessKey", typeof(string), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonAccessKeyPropertyChanged));

    /// <summary>
    /// Gets the <see cref="UIElement.AccessKey"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    /// <returns>The <see cref="string"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> access key.</returns>
    public static string GetSecondaryButtonAccessKey(SplitButton element)
    {
        return (string)element.GetValue(SecondaryButtonAccessKeyProperty);
    }

    /// <summary>
    /// Sets the <see cref="UIElement.AccessKey"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    public static void SetSecondaryButtonAccessKey(SplitButton element, string value)
    {
        element.SetValue(SecondaryButtonAccessKeyProperty, value);
    }

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="UIElement.KeyTipHorizontalOffset"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty SecondaryButtonKeyTipHorizontalOffsetProperty = DependencyProperty.RegisterAttached(
    //    "SecondaryButtonKeyTipHorizontalOffset", typeof(double), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonKeyTipHorizontalOffsetPropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="UIElement.KeyTipHorizontalOffset"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The horizontal offset <see cref="double"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> key tip.</returns>
    //public static double GetSecondaryButtonKeyTipHorizontalOffset(SplitButton element)
    //{
    //    return (double)element.GetValue(SecondaryButtonKeyTipHorizontalOffsetProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="UIElement.KeyTipHorizontalOffset"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetSecondaryButtonKeyTipHorizontalOffset(SplitButton element, double value)
    //{
    //    element.SetValue(SecondaryButtonKeyTipHorizontalOffsetProperty, value);
    //}

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="UIElement.KeyTipPlacementMode"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty SecondaryButtonKeyTipPlacementModeProperty = DependencyProperty.RegisterAttached(
    //    "SecondaryButtonKeyTipPlacementMode", typeof(KeyTipPlacementMode), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonKeyTipPlacementModePropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="UIElement.KeyTipPlacementMode"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The <see cref="KeyTipPlacementMode"/> <see cref="enum"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> key tip.</returns>
    //public static KeyTipPlacementMode GetSecondaryButtonKeyTipPlacementMode(SplitButton element)
    //{
    //    return (KeyTipPlacementMode)element.GetValue(SecondaryButtonKeyTipPlacementModeProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="UIElement.KeyTipPlacementMode"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetSecondaryButtonKeyTipPlacementMode(SplitButton element, KeyTipPlacementMode value)
    //{
    //    element.SetValue(SecondaryButtonKeyTipPlacementModeProperty, value);
    //}

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="UIElement.KeyTipTarget"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty SecondaryButtonKeyTipTargetProperty = DependencyProperty.RegisterAttached(
    //    "SecondaryButtonKeyTipTarget", typeof(UIElement), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonKeyTipTargetPropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="UIElement.KeyTipTarget"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The target <see cref="UIElement"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> key tip.</returns>
    //public static UIElement GetSecondaryButtonKeyTipTarget(SplitButton element)
    //{
    //    return (UIElement)element.GetValue(SecondaryButtonKeyTipTargetProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="UIElement.KeyTipTarget"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetSecondaryButtonKeyTipTarget(SplitButton element, UIElement value)
    //{
    //    element.SetValue(SecondaryButtonKeyTipTargetProperty, value);
    //}

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="UIElement.KeyTipVerticalOffset"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty SecondaryButtonKeyTipVerticalOffsetProperty = DependencyProperty.RegisterAttached(
    //    "SecondaryButtonKeyTipVerticalOffset", typeof(double), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonKeyTipVerticalOffsetPropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="UIElement.KeyTipVerticalOffset"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The vertical offset <see cref="double"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> key tip.</returns>
    //public static double GetSecondaryButtonKeyTipVerticalOffset(SplitButton element)
    //{
    //    return (double)element.GetValue(SecondaryButtonKeyTipVerticalOffsetProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="UIElement.KeyTipVerticalOffset"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetSecondaryButtonKeyTipVerticalOffset(SplitButton element, double value)
    //{
    //    element.SetValue(SecondaryButtonKeyTipVerticalOffsetProperty, value);
    //}

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding a <see cref="ToolTip"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    public static readonly DependencyProperty SecondaryButtonToolTipProperty = DependencyProperty.RegisterAttached(
        "SecondaryButtonToolTip", typeof(object), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonToolTipPropertyChanged));

    /// <summary>
    /// Gets the <see cref="ToolTip"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    /// <returns>The <see cref="object"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> tooltip.</returns>
    public static object GetSecondaryButtonToolTip(SplitButton element)
    {
        return (object)element.GetValue(SecondaryButtonToolTipProperty);
    }

    /// <summary>
    /// Sets the <see cref="ToolTip"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    /// </summary>
    public static void SetSecondaryButtonToolTip(SplitButton element, object value)
    {
        element.SetValue(SecondaryButtonToolTipProperty, value);
    }

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="ToolTipService.PlacementProperty"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty SecondaryButtonToolTipPlacementProperty = DependencyProperty.RegisterAttached(
    //    "SecondaryButtonToolTipPlacement", typeof(PlacementMode), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonToolTipPlacementPropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="ToolTipService.PlacementProperty"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The <see cref="PlacementMode"/> <see cref="enum"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> tooltip.</returns>
    //public static PlacementMode GetSecondaryButtonToolTipPlacement(SplitButton element)
    //{
    //    return (PlacementMode)element.GetValue(SecondaryButtonToolTipPlacementProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="ToolTipService.PlacementProperty"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetSecondaryButtonToolTipPlacement(SplitButton element, PlacementMode value)
    //{
    //    element.SetValue(SecondaryButtonToolTipPlacementProperty, value);
    //}

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="ToolTipService.PlacementTargetProperty"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty SecondaryButtonToolTipPlacementTargetProperty = DependencyProperty.RegisterAttached(
    //    "SecondaryButtonToolTipPlacementTarget", typeof(UIElement), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonToolTipPlacementTargetPropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="ToolTipService.PlacementTargetProperty"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The target <see cref="UIElement"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> tooltip placement.</returns>
    //public static UIElement GetSecondaryButtonToolTipPlacementTarget(SplitButton element)
    //{
    //    return (UIElement)element.GetValue(SecondaryButtonToolTipPlacementTargetProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="ToolTipService.PlacementTargetProperty"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetSecondaryButtonToolTipPlacementTarget(SplitButton element, UIElement value)
    //{
    //    element.SetValue(SecondaryButtonToolTipPlacementTargetProperty, value);
    //}

    ///// <summary>
    ///// Attached <see cref="DependencyProperty"/> for binding a <see cref="AutomationProperties.NameProperty"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static readonly DependencyProperty SecondaryButtonAutomationNameProperty = DependencyProperty.RegisterAttached(
    //    "SecondaryButtonAutomationName", typeof(string), typeof(SplitButtonExtensions), new PropertyMetadata(null, OnSecondaryButtonAutomationNamePropertyChanged));

    ///// <summary>
    ///// Gets the <see cref="AutomationProperties.NameProperty"/> for the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    ///// <returns>The <see cref="string"/> associated with the <see cref="SplitButton"/> secondary <see cref="Button"/> automation name.</returns>
    //public static string GetSecondaryButtonAutomationName(SplitButton element)
    //{
    //    return (string)element.GetValue(SecondaryButtonAutomationNameProperty);
    //}

    ///// <summary>
    ///// Sets the <see cref="AutomationProperties.NameProperty"/> to the secondary <see cref="Button"/> of the specified <see cref="SplitButton"/>
    ///// </summary>
    //public static void SetSecondaryButtonAutomationName(SplitButton element, string value)
    //{
    //    element.SetValue(SecondaryButtonAutomationNameProperty, value);
    //}

    private static void OnSplitButtonUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is SplitButton splitButton)
        {
            splitButton.Loaded -= ChangePrimaryButtonToolTip;
            //splitButton.Loaded -= ChangePrimaryButtonToolTipPlacement;
            //splitButton.Loaded -= ChangePrimaryButtonToolTipPlacementTarget;
            splitButton.Loaded -= ChangeSecondaryButtonAccessKey;
            //splitButton.Loaded -= ChangeSecondaryButtonKeyTipHorizontalOffset;
            //splitButton.Loaded -= ChangeSecondaryButtonKeyTipPlacementMode;
            //splitButton.Loaded -= ChangeSecondaryButtonKeyTipTarget;
            //splitButton.Loaded -= ChangeSecondaryButtonKeyTipVerticalOffset;
            splitButton.Loaded -= ChangeSecondaryButtonToolTip;
            //splitButton.Loaded -= ChangeSecondaryButtonToolTipPlacement;
            //splitButton.Loaded -= ChangeSecondaryButtonToolTipPlacementTarget;
            //splitButton.Loaded -= ChangeSecondaryButtonAutomationName;
            splitButton.Unloaded -= OnSplitButtonUnloaded;
        }
    }

    private static void OnPrimaryButtonToolTipPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is SplitButton splitButton)
        {
            splitButton.Loaded -= ChangePrimaryButtonToolTip;
            splitButton.Unloaded -= OnSplitButtonUnloaded;

            if (PrimaryButtonToolTipProperty != null)
            {
                splitButton.Loaded += ChangePrimaryButtonToolTip;
                splitButton.Unloaded += OnSplitButtonUnloaded;
            }
        }
    }

    private static void ChangePrimaryButtonToolTip(object sender, RoutedEventArgs args)
    {
        if (sender is SplitButton splitButton)
        {
            var primaryButton = splitButton.FindDescendant<Button>(pb => pb.Name == "PrimaryButton");
            if (primaryButton != null)
            {
                ToolTipService.SetToolTip(primaryButton, GetPrimaryButtonToolTip(splitButton));

                // Inherit the tooltip placement from the specified split button
                PlacementMode parentPlacementMode = ToolTipService.GetPlacement(splitButton);
                if (parentPlacementMode != PlacementMode.Top)
                {
                    ToolTipService.SetPlacement(primaryButton, parentPlacementMode);
                }
            }
        }
    }

    //private static void OnPrimaryButtonToolTipPlacementPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangePrimaryButtonToolTipPlacement;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (PrimaryButtonToolTipPlacementProperty != null)
    //        {
    //            splitButton.Loaded += ChangePrimaryButtonToolTipPlacement;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangePrimaryButtonToolTipPlacement(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var primaryButton = splitButton.FindDescendant<Button>(pb => pb.Name == "PrimaryButton");
    //        if (primaryButton != null)
    //        {
    //            ToolTipService.SetPlacement(primaryButton, GetPrimaryButtonToolTipPlacement(splitButton));
    //        }
    //    }
    //}

    //private static void OnPrimaryButtonToolTipPlacementTargetPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangePrimaryButtonToolTipPlacementTarget;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (PrimaryButtonToolTipPlacementTargetProperty != null)
    //        {
    //            splitButton.Loaded += ChangePrimaryButtonToolTipPlacementTarget;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangePrimaryButtonToolTipPlacementTarget(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var primaryButton = splitButton.FindDescendant<Button>(pb => pb.Name == "PrimaryButton");
    //        if (primaryButton != null)
    //        {
    //            ToolTipService.SetPlacementTarget(primaryButton, GetPrimaryButtonToolTipPlacementTarget(splitButton));
    //        }
    //    }
    //}

    private static void OnSecondaryButtonAccessKeyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is SplitButton splitButton)
        {
            splitButton.Loaded -= ChangeSecondaryButtonAccessKey;
            splitButton.Unloaded -= OnSplitButtonUnloaded;

            if (SecondaryButtonAccessKeyProperty != null)
            {
                splitButton.Loaded += ChangeSecondaryButtonAccessKey;
                splitButton.Unloaded += OnSplitButtonUnloaded;
            }
        }
    }

    private static void ChangeSecondaryButtonAccessKey(object sender, RoutedEventArgs args)
    {
        if (sender is SplitButton splitButton)
        {
            var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
            if (secondaryButton != null)
            {
                secondaryButton.AccessKey = GetSecondaryButtonAccessKey(splitButton);
                secondaryButton.ExitDisplayModeOnAccessKeyInvoked = false;

                // Fix for a potential overlap between the key tips
                if (splitButton.KeyTipPlacementMode == KeyTipPlacementMode.Right)
                {
                    secondaryButton.KeyTipPlacementMode = KeyTipPlacementMode.Auto;
                }
            }
        }
    }

    //private static void OnSecondaryButtonKeyTipHorizontalOffsetPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangeSecondaryButtonKeyTipHorizontalOffset;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (SecondaryButtonKeyTipHorizontalOffsetProperty != null)
    //        {
    //            splitButton.Loaded += ChangeSecondaryButtonKeyTipHorizontalOffset;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangeSecondaryButtonKeyTipHorizontalOffset(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
    //        if (secondaryButton != null)
    //        {
    //            secondaryButton.KeyTipHorizontalOffset = GetSecondaryButtonKeyTipHorizontalOffset(splitButton);
    //        }
    //    }
    //}

    //private static void OnSecondaryButtonKeyTipPlacementModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangeSecondaryButtonKeyTipPlacementMode;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (SecondaryButtonKeyTipPlacementModeProperty != null)
    //        {
    //            splitButton.Loaded += ChangeSecondaryButtonKeyTipPlacementMode;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangeSecondaryButtonKeyTipPlacementMode(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
    //        if (secondaryButton != null)
    //        {
    //            secondaryButton.KeyTipPlacementMode = GetSecondaryButtonKeyTipPlacementMode(splitButton);
    //        }
    //    }
    //}

    //private static void OnSecondaryButtonKeyTipTargetPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangeSecondaryButtonKeyTipTarget;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (SecondaryButtonKeyTipTargetProperty != null)
    //        {
    //            splitButton.Loaded += ChangeSecondaryButtonKeyTipTarget;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangeSecondaryButtonKeyTipTarget(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
    //        if (secondaryButton != null)
    //        {
    //            secondaryButton.KeyTipTarget = GetSecondaryButtonKeyTipTarget(splitButton);
    //        }
    //    }
    //}

    //private static void OnSecondaryButtonKeyTipVerticalOffsetPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangeSecondaryButtonKeyTipVerticalOffset;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (SecondaryButtonKeyTipVerticalOffsetProperty != null)
    //        {
    //            splitButton.Loaded += ChangeSecondaryButtonKeyTipVerticalOffset;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangeSecondaryButtonKeyTipVerticalOffset(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
    //        if (secondaryButton != null)
    //        {
    //            secondaryButton.KeyTipVerticalOffset = GetSecondaryButtonKeyTipVerticalOffset(splitButton);
    //        }
    //    }
    //}

    private static void OnSecondaryButtonToolTipPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is SplitButton splitButton)
        {
            splitButton.Loaded -= ChangeSecondaryButtonToolTip;
            splitButton.Unloaded -= OnSplitButtonUnloaded;

            if (SecondaryButtonToolTipProperty != null)
            {
                splitButton.Loaded += ChangeSecondaryButtonToolTip;
                splitButton.Unloaded += OnSplitButtonUnloaded;
            }
        }
    }

    private static void ChangeSecondaryButtonToolTip(object sender, RoutedEventArgs args)
    {
        if (sender is SplitButton splitButton)
        {
            var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
            if (secondaryButton != null)
            {
                ToolTipService.SetToolTip(secondaryButton, GetSecondaryButtonToolTip(splitButton));

                // Inherit the tooltip placement from the specified split button
                PlacementMode parentPlacementMode = ToolTipService.GetPlacement(splitButton);
                if (parentPlacementMode != PlacementMode.Top)
                {
                    ToolTipService.SetPlacement(secondaryButton, parentPlacementMode);
                }

                // Since ToolTip can accept either an object or a string, we verify its type before assigning it as a UI automation name
                if (GetSecondaryButtonToolTip(splitButton) is string secondaryButtonToolTip)
                {
                    AutomationProperties.SetName(secondaryButton, secondaryButtonToolTip);
                    AutomationProperties.SetAccessibilityView(secondaryButton, Windows.UI.Xaml.Automation.Peers.AccessibilityView.Content);
                }
            }
        }
    }

    //private static void OnSecondaryButtonToolTipPlacementPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangeSecondaryButtonToolTipPlacement;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (SecondaryButtonToolTipPlacementProperty != null)
    //        {
    //            splitButton.Loaded += ChangeSecondaryButtonToolTipPlacement;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangeSecondaryButtonToolTipPlacement(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
    //        if (secondaryButton != null)
    //        {
    //            ToolTipService.SetPlacement(secondaryButton, GetSecondaryButtonToolTipPlacement(splitButton));
    //        }
    //    }
    //}

    //private static void OnSecondaryButtonToolTipPlacementTargetPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangeSecondaryButtonToolTipPlacementTarget;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (SecondaryButtonToolTipPlacementTargetProperty != null)
    //        {
    //            splitButton.Loaded += ChangeSecondaryButtonToolTipPlacementTarget;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangeSecondaryButtonToolTipPlacementTarget(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
    //        if (secondaryButton != null)
    //        {
    //            ToolTipService.SetPlacementTarget(secondaryButton, GetSecondaryButtonToolTipPlacementTarget(splitButton));
    //        }
    //    }
    //}

    //private static void OnSecondaryButtonAutomationNamePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        splitButton.Loaded -= ChangeSecondaryButtonAutomationName;
    //        splitButton.Unloaded -= OnSplitButtonUnloaded;

    //        if (SecondaryButtonAutomationNameProperty != null)
    //        {
    //            splitButton.Loaded += ChangeSecondaryButtonAutomationName;
    //            splitButton.Unloaded += OnSplitButtonUnloaded;
    //        }
    //    }
    //}

    //private static void ChangeSecondaryButtonAutomationName(object sender, RoutedEventArgs args)
    //{
    //    if (sender is SplitButton splitButton)
    //    {
    //        var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
    //        if (secondaryButton != null)
    //        {
    //            AutomationProperties.SetAccessibilityView(secondaryButton, Windows.UI.Xaml.Automation.Peers.AccessibilityView.Content);
    //            AutomationProperties.SetName(secondaryButton, GetSecondaryButtonAutomationName(splitButton));
    //        }
    //    }
    //}
}
