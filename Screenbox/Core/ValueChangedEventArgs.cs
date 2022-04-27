using System;

namespace Screenbox.Core
{
    public class ValueChangedEventArgs<T> : EventArgs
    {
        public T NewValue { get; }

        public T OldValue { get; }

        public ValueChangedEventArgs(T newValue, T oldValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
}
