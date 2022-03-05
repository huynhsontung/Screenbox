#nullable enable

using System;

namespace Screenbox.Core
{
    internal class ProgressUpdatedEventArgs : EventArgs
    {
        public ProgressUpdatedEventArgs(string? title, string? text, bool isIndeterminate, double value)
        {
            Title = title;
            Text = text;
            IsIndeterminate = isIndeterminate;
            Value = value;
        }

        public string? Title { get; set; }

        public string? Text { get; set; }

        public double Value { get; set; }

        public bool IsIndeterminate { get; set; }
    }
}
