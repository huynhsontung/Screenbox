using System;
using System.Runtime.InteropServices;

namespace Screenbox.Interop.LibVLC.Native;

/// <summary>
/// Native mirror of <c>vlc_value_t</c>.
/// </summary>
/// <remarks>
/// Reference: <a href="https://videolan.videolan.me/vlc-3.0/unionvlc__value__t.html"/>
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 8)]
internal unsafe struct VlcValue
{
    [FieldOffset(0)]
    public long Integer;

    [FieldOffset(0)]
    public byte Boolean;

    [FieldOffset(0)]
    public float Float;

    [FieldOffset(0)]
    public IntPtr String;

    [FieldOffset(0)]
    public IntPtr Address;

    [FieldOffset(0)]
    public VlcList* List;

    [FieldOffset(0)]
    public VlcCoordinates Coordinates;
}
