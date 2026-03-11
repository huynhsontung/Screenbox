#nullable enable

using LibVLCSharp.Shared;

namespace Screenbox.Core.Services;

/// <summary>
/// Provides VLC dialog handler setup for media playback.
/// </summary>
public interface IVlcDialogService
{
    /// <summary>
    /// Registers dialog handlers for the specified <see cref="LibVLC"/> instance to handle
    /// VLC-specific dialogs such as login prompts, error messages, and progress updates.
    /// </summary>
    /// <param name="libVlc">The <see cref="LibVLC"/> instance to configure dialog handlers for.</param>
    void SetVlcDialogHandlers(LibVLC libVlc);
}
