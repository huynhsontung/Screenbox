// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Original Source: https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/2.5.1/src/dxaml/xcp/components/XboxUtility/XboxUtility.cpp

using Windows.System;
using Windows.UI.Xaml.Input;

namespace Screenbox.UI.Input;

/// <summary>
/// Provides <see langword="static"/> helper methods for recognizing gamepad inputs
/// and mapping them to navigation actions.
/// </summary>
public static class XboxHelper
{
    /// <summary>
    /// Determines whether the specified virtual key is a gamepad navigation input.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key belongs to the gamepad navigation range;
    /// otherwise, <b><see langword="false"/></b>.
    /// </returns>
    public static bool IsGamepadNavigationInput(VirtualKey key)
    {
        return key is >= VirtualKey.GamepadA and <= VirtualKey.GamepadRightThumbstickLeft;
    }

    /// <summary>
    /// Determines whether the specified virtual key represents a directional gamepad
    /// navigation input.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key is a directional navigation input;
    /// otherwise, <b><see langword="false"/></b>.
    /// </returns>
    public static bool IsGamepadNavigationDirection(VirtualKey key)
    {
        return IsGamepadNavigationRight(key)
            || IsGamepadNavigationLeft(key)
            || IsGamepadNavigationUp(key)
            || IsGamepadNavigationDown(key);
    }

    /// <summary>
    /// Determines whether the specified virtual key represents a page navigation input.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key is a shoulder or trigger page navigation key;
    /// otherwise, <b><see langword="false"/></b>.
    /// </returns>
    public static bool IsGamepadPageNavigationDirection(VirtualKey key)
    {
        return key is VirtualKey.GamepadLeftShoulder
            or VirtualKey.GamepadRightShoulder
            or VirtualKey.GamepadLeftTrigger
            or VirtualKey.GamepadRightTrigger;
    }

    /// <summary>
    /// Determines whether the specified virtual key represents a rightward gamepad
    /// navigation input.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key indicates navigation to the right;
    /// otherwise, <b><see langword="false"/></b>.
    /// </returns>
    public static bool IsGamepadNavigationRight(VirtualKey key)
    {
        return key is VirtualKey.GamepadLeftThumbstickRight or VirtualKey.GamepadDPadRight;
    }

    /// <summary>
    /// Determines whether the specified virtual key represents a leftward gamepad
    /// navigation input.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key indicates navigation to the left;
    /// otherwise, <b><see langword="false"/></b>.
    /// </returns>
    public static bool IsGamepadNavigationLeft(VirtualKey key)
    {
        return key is VirtualKey.GamepadLeftThumbstickLeft or VirtualKey.GamepadDPadLeft;
    }

    /// <summary>
    /// Determines whether the specified virtual key represents an upward gamepad
    /// navigation input.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key indicates navigation upward;
    /// otherwise, <b><see langword="false"/></b>.
    /// </returns>
    public static bool IsGamepadNavigationUp(VirtualKey key)
    {
        return key is VirtualKey.GamepadLeftThumbstickUp or VirtualKey.GamepadDPadUp;
    }

    /// <summary>
    /// Determines whether the specified virtual key represents a downward gamepad
    /// navigation input.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key indicates navigation downward;
    /// otherwise, <b><see langword="false"/></b>.
    /// </returns>
    public static bool IsGamepadNavigationDown(VirtualKey key)
    {
        return key is VirtualKey.GamepadLeftThumbstickDown or VirtualKey.GamepadDPadDown;
    }

    /// <summary>
    /// Determines whether the specified virtual key corresponds to a gamepad navigation
    /// accept action.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key is the gamepad accept action;
    /// otherwise, <b><see langword="false"/></b>.
    /// </returns>
    public static bool IsGamepadNavigationAccept(VirtualKey key)
    {
        return key == VirtualKey.GamepadA;
    }

    /// <summary>
    /// Determines whether the specified virtual key corresponds to a gamepad navigation
    /// cancel action.
    /// </summary>
    /// <param name="key">The virtual key to inspect.</param>
    /// <returns>
    /// <b><see langword="true"/></b> if the key is the gamepad cancel action;
    /// otherwise, <b><see langword="false"/>.</b>
    /// </returns>
    public static bool IsGamepadNavigationCancel(VirtualKey key)
    {
        return key == VirtualKey.GamepadB;
    }

    /// <summary>
    /// Maps a gamepad page-navigation key to the corresponding focus navigation direction.
    /// </summary>
    /// <param name="key">The virtual key to map.</param>
    /// <returns>
    /// The matching <see cref="FocusNavigationDirection"/> value, or <see cref="FocusNavigationDirection.None"/>
    /// when the key is not recognized.
    /// </returns>
    public static FocusNavigationDirection GetPageNavigationDirection(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.GamepadLeftShoulder => FocusNavigationDirection.Left,
            VirtualKey.GamepadRightShoulder => FocusNavigationDirection.Right,
            VirtualKey.GamepadLeftTrigger => FocusNavigationDirection.Up,
            VirtualKey.GamepadRightTrigger => FocusNavigationDirection.Down,
            _ => FocusNavigationDirection.None,
        };
    }

    /// <summary>
    /// Maps a gamepad directional input to the corresponding focus navigation direction.
    /// </summary>
    /// <param name="key">The virtual key to map.</param>
    /// <returns>
    /// The matching <see cref="FocusNavigationDirection"/> value, or <see cref="FocusNavigationDirection.None"/>
    /// when the key is not recognized.
    /// </returns>
    public static FocusNavigationDirection GetNavigationDirection(VirtualKey key)
    {
        if (!IsGamepadNavigationDirection(key))
        {
            return FocusNavigationDirection.None;
        }

        // Source: https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/2.5.1/src/dxaml/xcp/components/focus/FocusSelection/FocusSelection.cpp
        return key switch
        {
            VirtualKey.GamepadDPadUp or VirtualKey.GamepadLeftThumbstickUp or VirtualKey.Up => FocusNavigationDirection.Up,
            VirtualKey.GamepadDPadDown or VirtualKey.GamepadLeftThumbstickDown or VirtualKey.Down => FocusNavigationDirection.Down,
            VirtualKey.GamepadDPadLeft or VirtualKey.GamepadLeftThumbstickLeft or VirtualKey.Left => FocusNavigationDirection.Left,
            VirtualKey.GamepadDPadRight or VirtualKey.GamepadLeftThumbstickRight or VirtualKey.Right => FocusNavigationDirection.Right,
            _ => FocusNavigationDirection.None
        };
    }
}
