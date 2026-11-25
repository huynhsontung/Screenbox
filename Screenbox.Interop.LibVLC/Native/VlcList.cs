using System.Runtime.InteropServices;

namespace Screenbox.Interop.LibVLC.Native;

/// <summary>
/// Native mirror of <c>vlc_list_t</c>.
/// </summary>
/// <remarks>
/// Reference: <a href="https://videolan.videolan.me/vlc-3.0/structvlc__list__t.html"/>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VlcList
{
    public int Type;
    public int Count;
    public VlcValue* Values;
}
