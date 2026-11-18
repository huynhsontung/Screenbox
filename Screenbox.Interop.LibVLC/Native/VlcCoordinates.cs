using System.Runtime.InteropServices;

namespace Screenbox.Interop.LibVLC.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct VlcCoordinates
{
    public int X;
    public int Y;
}
