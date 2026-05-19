namespace Screenbox.Core.Messages;

/// <summary>
/// Represents a message that is sent when the active visualizer changes.
/// </summary>
public sealed class VisualizerChangedMessage
{
    /// <summary>
    /// Gets the path of the selected visualizer.
    /// </summary>
    /// <value>
    /// The path of the selected visualizer, or an empty string if the default
    /// or none is selected.
    /// </value>
    public string Path { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualizerChangedMessage"/> class.
    /// </summary>
    /// <param name="path">The path of the selected visualizer. Use an empty string to indicate the default or none.</param>
    public VisualizerChangedMessage(string path)
    {
        Path = path;
    }
}
