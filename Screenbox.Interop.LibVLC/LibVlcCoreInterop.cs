using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Screenbox.Interop.LibVLC;

/// <summary>
/// P/Invoke bindings to <c>libvlccore</c> object variable functions for real-time subtitle variable updates.
/// </summary>
public static partial class LibVlcCoreInterop
{
    private const string LibraryName = "libvlccore";
    private const int VlcVarInteger = 0x0030;
    private const int VlcVarDoInherit = 0x8000;
    private const int VlcSuccess = 0;
    private const int VlcENoVar = -6;

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct VlcValue
    {
        [FieldOffset(0)]
        public long Integer;

        [FieldOffset(0)]
        public byte Boolean;

        [FieldOffset(0)]
        public float Float;

        [FieldOffset(0)]
        public IntPtr Pointer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct VlcList
    {
        public int Type;
        public int Count;
        public VlcValue* Values;
    }

    [LibraryImport(LibraryName, EntryPoint = "var_Create", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int VarCreate(IntPtr obj, string name, int type);

    [LibraryImport(LibraryName, EntryPoint = "var_SetChecked", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int VarSetChecked(IntPtr obj, string name, int expectedType, VlcValue val);

    [LibraryImport(LibraryName, EntryPoint = "vlc_list_children")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial VlcList* VlcListChildren(IntPtr obj);

    [LibraryImport(LibraryName, EntryPoint = "vlc_list_release")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial void VlcListRelease(VlcList* list);

    /// <summary>
    /// Sets a VLC integer object variable on <paramref name="obj"/>, creating it if it does not yet exist,
    /// and optionally updates existing child objects (such as active video outputs).
    /// </summary>
    /// <param name="obj">Pointer to a <c>vlc_object_t</c> (for example, <c>MediaPlayer.NativeReference</c>).</param>
    /// <param name="name">The VLC variable name.</param>
    /// <param name="value">The integer value to assign.</param>
    /// <param name="propagateToChildren">
    /// <see langword="true"/> to also apply the variable to direct child objects that already define it
    /// (required for variables like <c>sub-margin</c> that have a callback on <c>vout_thread_t</c>).
    /// </param>
    public static unsafe void SetIntegerVariable(IntPtr obj, string name, long value, bool propagateToChildren = false)
    {
        if (obj == IntPtr.Zero)
        {
            return;
        }

        VlcValue val = new() { Integer = value };
        int result = VarSetChecked(obj, name, VlcVarInteger, val);
        if (result == VlcENoVar && VarCreate(obj, name, VlcVarInteger | VlcVarDoInherit) == VlcSuccess)
        {
            VarSetChecked(obj, name, VlcVarInteger, val);
        }

        if (!propagateToChildren)
        {
            return;
        }

        VlcList* children = VlcListChildren(obj);
        if (children is null)
        {
            return;
        }

        try
        {
            for (int i = 0; i < children->Count; i++)
            {
                IntPtr child = children->Values[i].Pointer;
                if (child != IntPtr.Zero)
                {
                    VarSetChecked(child, name, VlcVarInteger, val);
                }
            }
        }
        finally
        {
            VlcListRelease(children);
        }
    }
}
