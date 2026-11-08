using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Windows.System;

namespace Screenbox.Core.Helpers;

/// <summary>
/// Resolves the <see cref="GamepadButtonAction"/> configured for a gamepad button.
/// </summary>
public static class GamepadMappingHelper
{
    /// <summary>
    /// Gets the action mapped to the specified gamepad key.
    /// </summary>
    /// <remarks>
    /// Only buttons are user configurable. Thumbstick directions use fixed mappings because
    /// the thumbsticks are also used for UI navigation and seek/volume shortcuts.
    /// </remarks>
    /// <param name="settings">The player settings to read the mappings from.</param>
    /// <param name="key">The gamepad key that was pressed.</param>
    /// <returns>
    /// The mapped action, or <see cref="GamepadButtonAction.None"/> if the key is not
    /// a gamepad button or has no mapping.
    /// </returns>
    public static GamepadButtonAction GetMappedAction(this PlayerSettingsContext settings, VirtualKey key) => key switch
    {
        VirtualKey.GamepadX => settings.GamepadXAction,
        VirtualKey.GamepadY => settings.GamepadYAction,
        VirtualKey.GamepadView => settings.GamepadViewAction,
        VirtualKey.GamepadMenu => settings.GamepadMenuAction,
        VirtualKey.GamepadLeftShoulder => settings.GamepadLeftShoulderAction,
        VirtualKey.GamepadRightShoulder => settings.GamepadRightShoulderAction,
        VirtualKey.GamepadLeftTrigger => settings.GamepadLeftTriggerAction,
        VirtualKey.GamepadRightTrigger => settings.GamepadRightTriggerAction,
        VirtualKey.GamepadLeftThumbstickButton => settings.GamepadLeftThumbstickButtonAction,
        VirtualKey.GamepadRightThumbstickButton => settings.GamepadRightThumbstickButtonAction,
        VirtualKey.GamepadRightThumbstickLeft => GamepadButtonAction.Rewind,
        VirtualKey.GamepadRightThumbstickRight => GamepadButtonAction.FastForward,
        VirtualKey.GamepadRightThumbstickUp => GamepadButtonAction.IncreaseVolume,
        VirtualKey.GamepadRightThumbstickDown => GamepadButtonAction.DecreaseVolume,
        _ => GamepadButtonAction.None
    };

    /// <summary>
    /// Determines whether the specified key is a gamepad button or direction.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><see langword="true"/> if the key belongs to the gamepad key range.</returns>
    public static bool IsGamepadKey(this VirtualKey key) =>
        key is >= VirtualKey.GamepadA and <= VirtualKey.GamepadRightThumbstickLeft;
}
