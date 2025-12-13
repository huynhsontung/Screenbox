using System;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Behaviors
{
    public class ListViewContextRequestedEventArgs : EventArgs
    {
        public SelectorItem Item { get; }

        public bool Handled { get; set; }

        public ListViewContextRequestedEventArgs(SelectorItem item)
        {
            Item = item;
        }
    }
}
