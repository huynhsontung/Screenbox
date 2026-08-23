// Copyright (c) Tung Huynh and Contributors. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Windows.UI.Xaml;

namespace Screenbox.UI.Controls;

public sealed partial class SplitButtonEx
{
    /// <summary>
    /// Identifies the <see cref="PrimaryButtonAccessKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PrimaryButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(PrimaryButtonAccessKey),
        typeof(string),
        typeof(SplitButtonEx),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the access key (mnemonic) for the primary button part.
    /// </summary>
    /// <value>The access key (mnemonic) for the primary button part.</value>
    public string? PrimaryButtonAccessKey
    {
        get { return (string?)GetValue(PrimaryButtonAccessKeyProperty); }
        set { SetValue(PrimaryButtonAccessKeyProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="PrimaryButtonToolTip"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PrimaryButtonToolTipProperty = DependencyProperty.Register(
        nameof(PrimaryButtonToolTip),
        typeof(object),
        typeof(SplitButtonEx),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the object or string content of the primary button part ToolTip.
    /// </summary>
    /// <value>The object's tooltip content. The default is <see langword="null"/></value>
    public object? PrimaryButtonToolTip
    {
        get { return (object?)GetValue(PrimaryButtonToolTipProperty); }
        set { SetValue(PrimaryButtonToolTipProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="SecondaryButtonAccessKey"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SecondaryButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(SecondaryButtonAccessKey),
        typeof(string),
        typeof(SplitButtonEx),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the access key (mnemonic) for the secondary button part.
    /// </summary>
    /// <value>The access key (mnemonic) for the secondary button part.</value>
    public string? SecondaryButtonAccessKey
    {
        get { return (string?)GetValue(SecondaryButtonAccessKeyProperty); }
        set { SetValue(SecondaryButtonAccessKeyProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="SecondaryButtonToolTip"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SecondaryButtonToolTipProperty = DependencyProperty.Register(
        nameof(SecondaryButtonToolTip),
        typeof(object),
        typeof(SplitButtonEx),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Gets or sets the object or string content of the secondary button part ToolTip.
    /// </summary>
    /// <value>The object's tooltip content. The default is <see langword="null"/></value>
    public object? SecondaryButtonToolTip
    {
        get { return (object?)GetValue(SecondaryButtonToolTipProperty); }
        set { SetValue(SecondaryButtonToolTipProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="IsSecondaryButtonTabStop"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsSecondaryButtonTabStopProperty = DependencyProperty.Register(
        nameof(IsSecondaryButtonTabStop),
        typeof(bool),
        typeof(SplitButtonEx),
        new PropertyMetadata(false, OnPropertyChanged));

    /// <summary>
    /// Gets or sets a value that indicates whether the secondary button part
    /// is included in tab navigation.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the part is included in tab navigation;
    /// otherwise, <see langword="false"/>. The default is <see langword="false"/>.
    /// </value>
    public bool IsSecondaryButtonTabStop
    {
        get { return (bool)GetValue(IsSecondaryButtonTabStopProperty); }
        set { SetValue(IsSecondaryButtonTabStopProperty, value); }
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var owner = (SplitButtonEx)d;
        owner.OnPropertyChanged(e);
    }
}
