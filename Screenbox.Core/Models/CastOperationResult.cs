#nullable enable

using Screenbox.Casting.Models;

namespace Screenbox.Core.Models;

/// <summary>
/// Represents the outcome of a cast service operation.
/// </summary>
public sealed class CastOperationResult
{
    /// <summary>
    /// Initializes a new <see cref="CastOperationResult"/> instance.
    /// </summary>
    public CastOperationResult(bool succeeded, CastSessionState sessionState, string? errorMessage = null)
    {
        Succeeded = succeeded;
        SessionState = sessionState;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }

    public CastSessionState SessionState { get; }

    public string? ErrorMessage { get; }
}
