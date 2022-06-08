using LibVLCSharp.Shared;

namespace Screenbox.Core
{
    internal class PlayerStateChangedEventArgs : ValueChangedEventArgs<VLCState>
    {
        public PlayerStateChangedEventArgs(VLCState newValue, VLCState oldValue) : base(newValue, oldValue)
        {
        }
    }
}
