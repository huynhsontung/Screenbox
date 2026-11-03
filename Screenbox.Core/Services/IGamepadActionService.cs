using Screenbox.Core.Enums;

namespace Screenbox.Core.Services;

/// <summary>
/// Executes the action bound to a gamepad button.
/// </summary>
public interface IGamepadActionService
{
    /// <summary>
    /// Executes the specified gamepad action.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>
    /// <see langword="true"/> if the action was handled; <see langword="false"/> if the action
    /// must be handled by the player view, such as opening a flyout.
    /// </returns>
    bool TryExecute(GamepadButtonAction action);
}
