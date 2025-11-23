using System;
using Windows.System;
using Windows.UI.Xaml.Input;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods for converting a <see cref="KeyboardAccelerator"/> and its components
/// into a localized string that is suitable for display to the user.
/// </summary>
public static class KeyboardAcceleratorHelper
{
    /// <summary>The Equals (=) and Plus (+) key or button for any country/region (VK_OEM_PLUS).</summary>
    /// <remarks>
    /// If used as a key on an AppBarButton or MenuFlyoutItem, the <c>KeyboardAcceleratorTextOverride</c>
    /// property must be set; otherwise, the app will crash.
    /// </remarks>
    public const VirtualKey Plus = (VirtualKey)0xBB;
    /// <summary>The Comma (,) and Less Than (<) key or button for any country/region (VK_OEM_COMMA).</summary>
    /// <remarks>
    /// If used as a key on an AppBarButton or MenuFlyoutItem, the <c>KeyboardAcceleratorTextOverride</c>
    /// property must be set; otherwise, the app will crash.
    /// </remarks>
    public const VirtualKey Comma = (VirtualKey)0xBC;
    /// <summary>The Dash (-) and Underscore (_) key or button for any country/region (VK_OEM_MINUS).</summary>
    /// <remarks>
    /// If used as a key on an AppBarButton or MenuFlyoutItem, the <c>KeyboardAcceleratorTextOverride</c>
    /// property must be set; otherwise, the app will crash.
    /// </remarks>
    public const VirtualKey Minus = (VirtualKey)0xBD;
    /// <summary>The Period (.) and Greater Than (>) key or button for any country/region (VK_OEM_PERIOD).</summary>
    /// <remarks>
    /// If used as a key on an AppBarButton or MenuFlyoutItem, the <c>KeyboardAcceleratorTextOverride</c>
    /// property must be set; otherwise, the app will crash.
    /// </remarks>
    public const VirtualKey Period = (VirtualKey)0xBE;

    /// <summary>
    /// Converts the value of the specified <see cref="KeyboardAccelerator"/> to its equivalent
    /// <see cref="string"/> representation.
    /// </summary>
    /// <param name="value">The <see cref="KeyboardAccelerator"/> to convert.</param>
    /// <returns>The localized string representation of <paramref name="value"/>,
    /// or an empty string if the <paramref name="value"/> is <see langword="null"/>.</returns>
    public static string ToDisplayName(this KeyboardAccelerator value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string keyText = value.Key.ToDisplayName();
        string separator = Strings.KeyboardResources.KeyboardAcceleratorValueSeparator;
        var modifiers = value.Modifiers;

        string[] parts = new string[5];
        int count = 0;

        // Modifier keys in the tooltip appear in a different sequence than specified by the enum.
        if ((modifiers & VirtualKeyModifiers.Control) != 0)
        {
            parts[count++] = VirtualKeyModifiers.Control.ToDisplayName();
        }
        if ((modifiers & VirtualKeyModifiers.Menu) != 0)
        {
            parts[count++] = VirtualKeyModifiers.Menu.ToDisplayName();
        }
        if ((modifiers & VirtualKeyModifiers.Windows) != 0)
        {
            parts[count++] = VirtualKeyModifiers.Windows.ToDisplayName();
        }
        if ((modifiers & VirtualKeyModifiers.Shift) != 0)
        {
            parts[count++] = VirtualKeyModifiers.Shift.ToDisplayName();
        }

        // Avoid adding the key if it is already represented as a modifier.
        if (!string.IsNullOrEmpty(keyText) && Array.IndexOf(parts, keyText, 0, count) == -1)
        {
            parts[count++] = keyText;
        }

        return string.Join(separator, parts, 0, count);
    }

    /// <summary>
    /// Converts the value of the specified <see cref="VirtualKey"/> to its equivalent
    /// string representation.
    /// </summary>
    /// <param name="value">The <see cref="VirtualKey"/> to convert.</param>
    /// <returns>The localized string representation of <paramref name="value"/>.</returns>
    private static string ToDisplayName(this VirtualKey value)
    {
        return value switch
        {
            VirtualKey.None => string.Empty, // Only the AppBarButton/AppBarToggleButton and MenuFlyoutItem/ToggleMenuFlyoutItem tooltip returns a value.
            VirtualKey.LeftButton => Strings.KeyboardResources.VirtualKeyLeftButton,
            VirtualKey.RightButton => Strings.KeyboardResources.VirtualKeyRightButton,
            VirtualKey.Cancel => Strings.KeyboardResources.VirtualKeyCancel,
            VirtualKey.MiddleButton => Strings.KeyboardResources.VirtualKeyMiddleButton,
            VirtualKey.XButton1 => Strings.KeyboardResources.VirtualKeyXButton1,
            VirtualKey.XButton2 => Strings.KeyboardResources.VirtualKeyXButton2,
            VirtualKey.Back => Strings.KeyboardResources.VirtualKeyBack,
            VirtualKey.Tab => Strings.KeyboardResources.VirtualKeyTab,
            VirtualKey.Clear => Strings.KeyboardResources.VirtualKeyClear,
            VirtualKey.Enter => Strings.KeyboardResources.VirtualKeyEnter,
            VirtualKey.Shift => Strings.KeyboardResources.VirtualKeyModifiersShift,
            VirtualKey.Control => Strings.KeyboardResources.VirtualKeyModifiersControl,
            VirtualKey.Menu => Strings.KeyboardResources.VirtualKeyModifiersMenu,
            VirtualKey.Pause => Strings.KeyboardResources.VirtualKeyPause,
            VirtualKey.CapitalLock => Strings.KeyboardResources.VirtualKeyCapitalLock,
            VirtualKey.Hangul or VirtualKey.Kana => Strings.KeyboardResources.VirtualKeyHangul,
            VirtualKey.Junja => Strings.KeyboardResources.VirtualKeyJunja,
            VirtualKey.Final => Strings.KeyboardResources.VirtualKeyFinal,
            VirtualKey.Hanja or VirtualKey.Kanji => Strings.KeyboardResources.VirtualKeyHanja,
            VirtualKey.Escape => Strings.KeyboardResources.VirtualKeyEscape,
            VirtualKey.Convert => Strings.KeyboardResources.VirtualKeyConvert,
            VirtualKey.NonConvert => Strings.KeyboardResources.VirtualKeyNonConvert,
            VirtualKey.Accept => Strings.KeyboardResources.VirtualKeyAccept,
            VirtualKey.ModeChange => Strings.KeyboardResources.VirtualKeyModeChange,
            VirtualKey.Space => Strings.KeyboardResources.VirtualKeySpace,
            VirtualKey.PageUp => Strings.KeyboardResources.VirtualKeyPageUp,
            VirtualKey.PageDown => Strings.KeyboardResources.VirtualKeyPageDown,
            VirtualKey.End => Strings.KeyboardResources.VirtualKeyEnd,
            VirtualKey.Home => Strings.KeyboardResources.VirtualKeyHome,
            VirtualKey.Left => Strings.KeyboardResources.VirtualKeyLeft,
            VirtualKey.Up => Strings.KeyboardResources.VirtualKeyUp,
            VirtualKey.Right => Strings.KeyboardResources.VirtualKeyRight,
            VirtualKey.Down => Strings.KeyboardResources.VirtualKeyDown,
            VirtualKey.Select => Strings.KeyboardResources.VirtualKeySelect,
            VirtualKey.Print => Strings.KeyboardResources.VirtualKeyPrint,
            VirtualKey.Execute => Strings.KeyboardResources.VirtualKeyExecute,
            VirtualKey.Snapshot => Strings.KeyboardResources.VirtualKeySnapshot,
            VirtualKey.Insert => Strings.KeyboardResources.VirtualKeyInsert,
            VirtualKey.Delete => Strings.KeyboardResources.VirtualKeyDelete,
            VirtualKey.Help => Strings.KeyboardResources.VirtualKeyHelp,
            VirtualKey.Number0 => "0",
            VirtualKey.Number1 => "1",
            VirtualKey.Number2 => "2",
            VirtualKey.Number3 => "3",
            VirtualKey.Number4 => "4",
            VirtualKey.Number5 => "5",
            VirtualKey.Number6 => "6",
            VirtualKey.Number7 => "7",
            VirtualKey.Number8 => "8",
            VirtualKey.Number9 => "9",
            VirtualKey.LeftWindows => Strings.KeyboardResources.VirtualKeyModifiersWindows,
            VirtualKey.RightWindows => Strings.KeyboardResources.VirtualKeyModifiersWindows,
            VirtualKey.Application => Strings.KeyboardResources.VirtualKeyApplication,
            VirtualKey.Sleep => Strings.KeyboardResources.VirtualKeySleep,
            VirtualKey.NumberPad0 => Strings.KeyboardResources.VirtualKeyNumberPad0,
            VirtualKey.NumberPad1 => Strings.KeyboardResources.VirtualKeyNumberPad1,
            VirtualKey.NumberPad2 => Strings.KeyboardResources.VirtualKeyNumberPad2,
            VirtualKey.NumberPad3 => Strings.KeyboardResources.VirtualKeyNumberPad3,
            VirtualKey.NumberPad4 => Strings.KeyboardResources.VirtualKeyNumberPad4,
            VirtualKey.NumberPad5 => Strings.KeyboardResources.VirtualKeyNumberPad5,
            VirtualKey.NumberPad6 => Strings.KeyboardResources.VirtualKeyNumberPad6,
            VirtualKey.NumberPad7 => Strings.KeyboardResources.VirtualKeyNumberPad7,
            VirtualKey.NumberPad8 => Strings.KeyboardResources.VirtualKeyNumberPad8,
            VirtualKey.NumberPad9 => Strings.KeyboardResources.VirtualKeyNumberPad9,
            VirtualKey.Multiply => "*",
            VirtualKey.Add => "+",
            VirtualKey.Separator => Strings.KeyboardResources.VirtualKeySeparator,
            VirtualKey.Subtract => "-",
            VirtualKey.Decimal => ".",
            VirtualKey.Divide => "/",
            VirtualKey.NumberKeyLock => Strings.KeyboardResources.VirtualKeyNumberKeyLock,
            VirtualKey.Scroll => Strings.KeyboardResources.VirtualKeyScroll,
            VirtualKey.LeftShift => Strings.KeyboardResources.VirtualKeyModifiersShift,
            VirtualKey.RightShift => Strings.KeyboardResources.VirtualKeyModifiersShift,
            VirtualKey.LeftControl => Strings.KeyboardResources.VirtualKeyModifiersControl,
            VirtualKey.RightControl => Strings.KeyboardResources.VirtualKeyModifiersControl,
            VirtualKey.LeftMenu => Strings.KeyboardResources.VirtualKeyModifiersMenu,
            VirtualKey.RightMenu => Strings.KeyboardResources.VirtualKeyModifiersMenu,
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Converts the value of the specified <see cref="VirtualKeyModifiers"/> to its equivalent
    /// string representation.
    /// </summary>
    /// <param name="value">The <see cref="VirtualKeyModifiers"/> to convert (single flag only).</param>
    /// <returns>The localized string representation of <paramref name="value"/>.</returns>
    private static string ToDisplayName(this VirtualKeyModifiers value)
    {
        return value switch
        {
            VirtualKeyModifiers.Control => Strings.KeyboardResources.VirtualKeyModifiersControl,
            VirtualKeyModifiers.Menu => Strings.KeyboardResources.VirtualKeyModifiersMenu,
            VirtualKeyModifiers.Shift => Strings.KeyboardResources.VirtualKeyModifiersShift,
            VirtualKeyModifiers.Windows => Strings.KeyboardResources.VirtualKeyModifiersWindows,
            _ => string.Empty
        };
    }
}
