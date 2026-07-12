using System;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

public static class SwapChainPanelExtensions
{
    private static readonly Guid ISwapChainPanelNative_IID = new("f92f19d2-3ade-45a6-a20c-f6f1ea90554b");

    public static unsafe int SetSwapChain(this SwapChainPanel panel, IntPtr swapChainPtr)
    {
        var winrtObj = (WinRT.IWinRTObject)panel;
        IntPtr nativePtr = winrtObj.NativeObject.ThisPtr;

        Guid iid = ISwapChainPanelNative_IID;
        int hr = Marshal.QueryInterface(nativePtr, in iid, out IntPtr swapChainPanelNativePtr);
        if (hr != 0) return hr;

        try
        {
            // COM vtable layout for ISwapChainPanelNative:
            // [0] QueryInterface
            // [1] AddRef
            // [2] Release
            // [3] SetSwapChain(IntPtr swapChain)
            void** vtbl = (void**)swapChainPanelNativePtr;
            var setSwapChainFunc = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)vtbl[3];
            return setSwapChainFunc(swapChainPanelNativePtr, swapChainPtr);
        }
        finally
        {
            Marshal.Release(swapChainPanelNativePtr);
        }
    }
}
