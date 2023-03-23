using Windows.Storage;

namespace Screenbox.Core.Messages
{
    public sealed class RaiseLibraryAccessDeniedNotificationMessage
    {
        public KnownLibraryId Library { get; }

        public RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId library)
        {
            Library = library;
        }
    }
}
