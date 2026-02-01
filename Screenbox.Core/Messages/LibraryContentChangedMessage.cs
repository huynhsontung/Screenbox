using Windows.Storage;

namespace Screenbox.Core.Messages;

/// <summary>
/// Message indicating that the library content has changed
/// </summary>
public sealed class LibraryContentChangedMessage
{
    public KnownLibraryId LibraryId { get; }

    public LibraryContentChangedMessage(KnownLibraryId libraryId)
    {
        LibraryId = libraryId;
    }
}
