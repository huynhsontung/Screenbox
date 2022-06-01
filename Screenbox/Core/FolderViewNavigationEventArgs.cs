using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Screenbox.Core
{
    internal class FolderViewNavigationEventArgs : EventArgs
    {
        public IReadOnlyList<StorageFolder> Breadcrumbs { get; }

        public FolderViewNavigationEventArgs(IReadOnlyList<StorageFolder> breadcrumbs)
        {
            Breadcrumbs = breadcrumbs;
        }
    }
}
