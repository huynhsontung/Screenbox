using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Screenbox.Interop.LibVLC.Native;

internal static partial class LibVlcCoreNativeMethods
{
    [LibraryImport(Constants.CoreLibraryName, EntryPoint = "var_Create", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int VarCreate(IntPtr obj, string name, int type);

    [LibraryImport(Constants.CoreLibraryName, EntryPoint = "var_SetChecked", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int VarSetChecked(IntPtr obj, string name, int expectedType, VlcValue val);

    [LibraryImport(Constants.CoreLibraryName, EntryPoint = "vlc_list_children")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial VlcList* VlcListChildren(IntPtr obj);

    [LibraryImport(Constants.CoreLibraryName, EntryPoint = "vlc_list_release")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial void VlcListRelease(VlcList* list);
}
