using System;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Behaviors;

/// <summary>
/// Provides event data for the <see cref="ListViewContextBehavior.ContextRequested"/> event.
/// </summary>
public class ListViewContextRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the <see cref="SelectorItem"/> that the context request applies to.
    /// </summary>
    /// <value>The item that raised the event.</value>
    public SelectorItem Item { get; }

    /// <summary>
    /// Gets or sets a value that marks the context requested event as handled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to mark the context requested event handled; <see langword="false"/>
    /// to leave the event unhandled and be acted on by the behavior. The default is <see langword="false"/>.
    /// </value>
    public bool Handled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListViewContextRequestedEventArgs"/> class
    /// with the specified <see cref="SelectorItem"/>.
    /// </summary>
    /// <param name="item">The <see cref="SelectorItem"/> that the event is associated with.</param>
    public ListViewContextRequestedEventArgs(SelectorItem item)
    {
        Item = item;
    }
}
