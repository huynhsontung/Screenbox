namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents an item container that wraps a canonical <see cref="MediaViewModel"/>
/// (for example, a play queue slot or playlist entry).
/// </summary>
public interface IMediaContainer
{
    /// <summary>
    /// Gets the underlying canonical media view model.
    /// </summary>
    MediaViewModel Media { get; }
}
