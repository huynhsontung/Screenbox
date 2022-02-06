using System;

namespace Screenbox.Core
{
    internal class ProgressUpdatedEventArgs : EventArgs
    {
        public string Title { get; set; }

        public string Text { get; set; }

        public double Value { get; set; }

        public bool IsIndeterminate { get; set; }
    }
}
