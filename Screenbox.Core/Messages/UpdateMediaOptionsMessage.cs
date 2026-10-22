namespace Screenbox.Core.Messages;

using Screenbox.Core.ViewModels;

/// <summary>
/// Sent to update playback options for a media item.
/// </summary>
public sealed record UpdateMediaOptionsMessage(MediaViewModel Media, string Options);
