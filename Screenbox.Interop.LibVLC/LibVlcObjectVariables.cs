using System;
using Screenbox.Interop.LibVLC.Native;
using static Screenbox.Interop.LibVLC.Native.Constants;
using static Screenbox.Interop.LibVLC.Native.LibVlcCoreNativeMethods;

namespace Screenbox.Interop.LibVLC;

/// <summary>
/// Provides access to VLC object variables.
/// </summary>
public static class LibVlcObjectVariables
{
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
                IntPtr child = children->Values[i].Address;
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
