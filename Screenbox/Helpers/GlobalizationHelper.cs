using System;
using System.Globalization;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods related to globalization.
/// </summary>
public static class GlobalizationHelper
{
    private const string VirtualKeyModifiersControlResourceName = "VirtualKeyModifiersControl";
    private const string VirtualKeyModifiersMenuResourceName = "VirtualKeyModifiersMenu";
    private const string VirtualKeyModifiersShiftResourceName = "VirtualKeyModifiersShift";
    private const string VirtualKeyModifiersWindowsResourceName = "VirtualKeyModifiersWindows";

    private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse("KeyboardResources");

    private static readonly bool _isKeyboardAcceleratorMirrored =
        CultureInfo.CurrentCulture.TwoLetterISOLanguageName is "ar" or "fa";

    /// <summary>
    /// Gets whether the text direction for the current app's language is right-to-left (RTL).
    /// </summary>
    /// <returns><see langword="true"/>, if the text direction is right-to-left; otherwise, <see langword="false"/>.</returns>
    public static readonly bool IsRightToLeftLanguage = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

    /// <summary>
    /// Gets the <see cref="FlowDirection"/> based on the text directionality of the app's display language.
    /// </summary>
    /// <returns>
    /// <see cref="FlowDirection.RightToLeft"/> if <see cref="IsRightToLeftLanguage"/> is <see langword="true"/>;
    /// otherwise, <see cref="FlowDirection.LeftToRight"/>.
    /// </returns>
    public static FlowDirection GetFlowDirection()
    {
        return IsRightToLeftLanguage ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    /// <summary>
    /// Converts the value of the specified <see cref="KeyboardAccelerator"/> to its equivalent
    /// <see cref="string"/> representation.
    /// </summary>
    /// <param name="value">The <see cref="KeyboardAccelerator"/> to convert.</param>
    /// <returns>The string representation of <paramref name="value"/>. Empty string if the <paramref name="value"/> is <see langword="null"/>.</returns>
    public static string GetKeyboardAcceleratorDisplayName(this KeyboardAccelerator value)
    {
        const string KeyboardAcceleratorValueSeparatorResourceName = "KeyboardAcceleratorValueSeparator";

        if (value is null)
        {
            return string.Empty;
        }

        string keyText = GetVirtualKeyDisplayName(value.Key);
        string separator = _resourceLoader.GetString(KeyboardAcceleratorValueSeparatorResourceName);
        var modifiers = value.Modifiers;

        string[] parts = new string[5];
        int count = 0;

        // Modifier keys in the tooltip appear in a different sequence than specified by the enum.
        if ((modifiers & VirtualKeyModifiers.Control) != 0)
        {
            parts[count++] = GetVirtualKeyModifiersDisplayName(VirtualKeyModifiers.Control);
        }
        if ((modifiers & VirtualKeyModifiers.Menu) != 0)
        {
            parts[count++] = GetVirtualKeyModifiersDisplayName(VirtualKeyModifiers.Menu);
        }
        if ((modifiers & VirtualKeyModifiers.Windows) != 0)
        {
            parts[count++] = GetVirtualKeyModifiersDisplayName(VirtualKeyModifiers.Windows);
        }
        if ((modifiers & VirtualKeyModifiers.Shift) != 0)
        {
            parts[count++] = GetVirtualKeyModifiersDisplayName(VirtualKeyModifiers.Shift);
        }

        // Avoid adding the key if it is already represented as a modifier.
        if (!string.IsNullOrEmpty(keyText) && Array.IndexOf(parts, keyText, 0, count) == -1)
        {
            parts[count++] = keyText;
        }

        if (_isKeyboardAcceleratorMirrored && count > 1)
        {
            Array.Reverse(parts, 0, count);
        }

        return string.Join(separator, parts, 0, count);
    }

    /// <summary>
    /// Gets the localized string for a <see cref="VirtualKey"/> value.
    /// </summary>
    /// <param name="value">The <see cref="VirtualKey"/> to convert.</param>
    /// <returns>The localized name of the virtual key.</returns>
    private static string GetVirtualKeyDisplayName(this VirtualKey value)
    {
        //const string NoneResourceName = "VirtualKeyNone";
        const string LeftButtonResourceName = "VirtualKeyLeftButton";
        const string RightButtonResourceName = "VirtualKeyRightButton";
        const string CancelResourceName = "VirtualKeyCancel";
        const string MiddleButtonResourceName = "VirtualKeyMiddleButton";
        const string XButton1ResourceName = "VirtualKeyXButton1";
        const string XButton2ResourceName = "VirtualKeyXButton2";
        const string BackResourceName = "VirtualKeyBack";
        const string TabResourceName = "VirtualKeyTab";
        const string ClearResourceName = "VirtualKeyClear";
        const string EnterResourceName = "VirtualKeyEnter";
        const string PauseResourceName = "VirtualKeyPause";
        const string CapitalLockResourceName = "VirtualKeyCapitalLock";
        const string HangulResourceName = "VirtualKeyHangul";
        const string JunjaResourceName = "VirtualKeyJunja";
        const string FinalResourceName = "VirtualKeyFinal";
        const string HanjaResourceName = "VirtualKeyHanja";
        const string EscapeResourceName = "VirtualKeyEscape";
        const string ConvertResourceName = "VirtualKeyConvert";
        const string NonConvertResourceName = "VirtualKeyNonConvert";
        const string AcceptResourceName = "VirtualKeyAccept";
        const string ModeChangeResourceName = "VirtualKeyModeChange";
        const string SpaceResourceName = "VirtualKeySpace";
        const string PageUpResourceName = "VirtualKeyPageUp";
        const string PageDownResourceName = "VirtualKeyPageDown";
        const string EndResourceName = "VirtualKeyEnd";
        const string HomeResourceName = "VirtualKeyHome";
        const string LeftResourceName = "VirtualKeyLeft";
        const string UpResourceName = "VirtualKeyUp";
        const string RightResourceName = "VirtualKeyRight";
        const string DownResourceName = "VirtualKeyDown";
        const string SelectResourceName = "VirtualKeySelect";
        const string PrintResourceName = "VirtualKeyPrint";
        const string ExecuteResourceName = "VirtualKeyExecute";
        const string SnapshotResourceName = "VirtualKeySnapshot";
        const string InsertResourceName = "VirtualKeyInsert";
        const string DeleteResourceName = "VirtualKeyDelete";
        const string HelpResourceName = "VirtualKeyHelp";
        const string Number0ResourceName = "VirtualKeyNumber0";
        const string Number1ResourceName = "VirtualKeyNumber1";
        const string Number2ResourceName = "VirtualKeyNumber2";
        const string Number3ResourceName = "VirtualKeyNumber3";
        const string Number4ResourceName = "VirtualKeyNumber4";
        const string Number5ResourceName = "VirtualKeyNumber5";
        const string Number6ResourceName = "VirtualKeyNumber6";
        const string Number7ResourceName = "VirtualKeyNumber7";
        const string Number8ResourceName = "VirtualKeyNumber8";
        const string Number9ResourceName = "VirtualKeyNumber9";
        const string ApplicationResourceName = "VirtualKeyApplication";
        const string SleepResourceName = "VirtualKeySleep";
        const string NumberPad0ResourceName = "VirtualKeyNumberPad0";
        const string NumberPad1ResourceName = "VirtualKeyNumberPad1";
        const string NumberPad2ResourceName = "VirtualKeyNumberPad2";
        const string NumberPad3ResourceName = "VirtualKeyNumberPad3";
        const string NumberPad4ResourceName = "VirtualKeyNumberPad4";
        const string NumberPad5ResourceName = "VirtualKeyNumberPad5";
        const string NumberPad6ResourceName = "VirtualKeyNumberPad6";
        const string NumberPad7ResourceName = "VirtualKeyNumberPad7";
        const string NumberPad8ResourceName = "VirtualKeyNumberPad8";
        const string NumberPad9ResourceName = "VirtualKeyNumberPad9";
        const string SeparatorResourceName = "VirtualKeySeparator";
        const string NumberKeyLockResourceName = "VirtualKeyNumberKeyLock";
        const string ScrollResourceName = "VirtualKeyScroll";

        return value switch
        {
            VirtualKey.None => string.Empty, // Only the AppBarButton/AppBarToggleButton and MenuFlyoutItem/ToggleMenuFlyoutItem tooltip returns a value.
            VirtualKey.LeftButton => _resourceLoader.GetString(LeftButtonResourceName),
            VirtualKey.RightButton => _resourceLoader.GetString(RightButtonResourceName),
            VirtualKey.Cancel => _resourceLoader.GetString(CancelResourceName),
            VirtualKey.MiddleButton => _resourceLoader.GetString(MiddleButtonResourceName),
            VirtualKey.XButton1 => _resourceLoader.GetString(XButton1ResourceName),
            VirtualKey.XButton2 => _resourceLoader.GetString(XButton2ResourceName),
            VirtualKey.Back => _resourceLoader.GetString(BackResourceName),
            VirtualKey.Tab => _resourceLoader.GetString(TabResourceName),
            VirtualKey.Clear => _resourceLoader.GetString(ClearResourceName),
            VirtualKey.Enter => _resourceLoader.GetString(EnterResourceName),
            VirtualKey.Shift => _resourceLoader.GetString(VirtualKeyModifiersShiftResourceName),
            VirtualKey.Control => _resourceLoader.GetString(VirtualKeyModifiersControlResourceName),
            VirtualKey.Menu => _resourceLoader.GetString(VirtualKeyModifiersMenuResourceName),
            VirtualKey.Pause => _resourceLoader.GetString(PauseResourceName),
            VirtualKey.CapitalLock => _resourceLoader.GetString(CapitalLockResourceName),
            VirtualKey.Hangul | VirtualKey.Kana => _resourceLoader.GetString(HangulResourceName),
            VirtualKey.Junja => _resourceLoader.GetString(JunjaResourceName),
            VirtualKey.Final => _resourceLoader.GetString(FinalResourceName),
            VirtualKey.Hanja | VirtualKey.Kanji => _resourceLoader.GetString(HanjaResourceName),
            VirtualKey.Escape => _resourceLoader.GetString(EscapeResourceName),
            VirtualKey.Convert => _resourceLoader.GetString(ConvertResourceName),
            VirtualKey.NonConvert => _resourceLoader.GetString(NonConvertResourceName),
            VirtualKey.Accept => _resourceLoader.GetString(AcceptResourceName),
            VirtualKey.ModeChange => _resourceLoader.GetString(ModeChangeResourceName),
            VirtualKey.Space => _resourceLoader.GetString(SpaceResourceName),
            VirtualKey.PageUp => _resourceLoader.GetString(PageUpResourceName),
            VirtualKey.PageDown => _resourceLoader.GetString(PageDownResourceName),
            VirtualKey.End => _resourceLoader.GetString(EndResourceName),
            VirtualKey.Home => _resourceLoader.GetString(HomeResourceName),
            VirtualKey.Left => _resourceLoader.GetString(LeftResourceName),
            VirtualKey.Up => _resourceLoader.GetString(UpResourceName),
            VirtualKey.Right => _resourceLoader.GetString(RightResourceName),
            VirtualKey.Down => _resourceLoader.GetString(DownResourceName),
            VirtualKey.Select => _resourceLoader.GetString(SelectResourceName),
            VirtualKey.Print => _resourceLoader.GetString(PrintResourceName),
            VirtualKey.Execute => _resourceLoader.GetString(ExecuteResourceName),
            VirtualKey.Snapshot => _resourceLoader.GetString(SnapshotResourceName),
            VirtualKey.Insert => _resourceLoader.GetString(InsertResourceName),
            VirtualKey.Delete => _resourceLoader.GetString(DeleteResourceName),
            VirtualKey.Help => _resourceLoader.GetString(HelpResourceName),
            VirtualKey.Number0 => _resourceLoader.GetString(Number0ResourceName),
            VirtualKey.Number1 => _resourceLoader.GetString(Number1ResourceName),
            VirtualKey.Number2 => _resourceLoader.GetString(Number2ResourceName),
            VirtualKey.Number3 => _resourceLoader.GetString(Number3ResourceName),
            VirtualKey.Number4 => _resourceLoader.GetString(Number4ResourceName),
            VirtualKey.Number5 => _resourceLoader.GetString(Number5ResourceName),
            VirtualKey.Number6 => _resourceLoader.GetString(Number6ResourceName),
            VirtualKey.Number7 => _resourceLoader.GetString(Number7ResourceName),
            VirtualKey.Number8 => _resourceLoader.GetString(Number8ResourceName),
            VirtualKey.Number9 => _resourceLoader.GetString(Number9ResourceName),
            VirtualKey.LeftWindows => _resourceLoader.GetString(VirtualKeyModifiersWindowsResourceName),
            VirtualKey.RightWindows => _resourceLoader.GetString(VirtualKeyModifiersWindowsResourceName),
            VirtualKey.Application => _resourceLoader.GetString(ApplicationResourceName),
            VirtualKey.Sleep => _resourceLoader.GetString(SleepResourceName),
            VirtualKey.NumberPad0 => _resourceLoader.GetString(NumberPad0ResourceName),
            VirtualKey.NumberPad1 => _resourceLoader.GetString(NumberPad1ResourceName),
            VirtualKey.NumberPad2 => _resourceLoader.GetString(NumberPad2ResourceName),
            VirtualKey.NumberPad3 => _resourceLoader.GetString(NumberPad3ResourceName),
            VirtualKey.NumberPad4 => _resourceLoader.GetString(NumberPad4ResourceName),
            VirtualKey.NumberPad5 => _resourceLoader.GetString(NumberPad5ResourceName),
            VirtualKey.NumberPad6 => _resourceLoader.GetString(NumberPad6ResourceName),
            VirtualKey.NumberPad7 => _resourceLoader.GetString(NumberPad7ResourceName),
            VirtualKey.NumberPad8 => _resourceLoader.GetString(NumberPad8ResourceName),
            VirtualKey.NumberPad9 => _resourceLoader.GetString(NumberPad9ResourceName),
            VirtualKey.Multiply => "*",
            VirtualKey.Add => "+",
            VirtualKey.Separator => _resourceLoader.GetString(SeparatorResourceName),
            VirtualKey.Subtract => "-",
            VirtualKey.Decimal => ".",
            VirtualKey.Divide => "/",
            VirtualKey.NumberKeyLock => _resourceLoader.GetString(NumberKeyLockResourceName),
            VirtualKey.Scroll => _resourceLoader.GetString(ScrollResourceName),
            VirtualKey.LeftShift => _resourceLoader.GetString(VirtualKeyModifiersShiftResourceName),
            VirtualKey.RightShift => _resourceLoader.GetString(VirtualKeyModifiersShiftResourceName),
            VirtualKey.LeftControl => _resourceLoader.GetString(VirtualKeyModifiersControlResourceName),
            VirtualKey.RightControl => _resourceLoader.GetString(VirtualKeyModifiersControlResourceName),
            VirtualKey.LeftMenu => _resourceLoader.GetString(VirtualKeyModifiersMenuResourceName),
            VirtualKey.RightMenu => _resourceLoader.GetString(VirtualKeyModifiersMenuResourceName),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Gets the localized string for a <see cref="VirtualKeyModifiers"/> value.
    /// </summary>
    /// <param name="value">The <see cref="VirtualKeyModifiers"/> to convert (single flag only).</param>
    /// <returns>The localized name of the virtual key modifier.</returns>
    private static string GetVirtualKeyModifiersDisplayName(this VirtualKeyModifiers value)
    {
        return value switch
        {
            VirtualKeyModifiers.Control => _resourceLoader.GetString(VirtualKeyModifiersControlResourceName),
            VirtualKeyModifiers.Menu => _resourceLoader.GetString(VirtualKeyModifiersMenuResourceName),
            VirtualKeyModifiers.Shift => _resourceLoader.GetString(VirtualKeyModifiersShiftResourceName),
            VirtualKeyModifiers.Windows => _resourceLoader.GetString(VirtualKeyModifiersWindowsResourceName),
            _ => string.Empty
        };
    }
}
