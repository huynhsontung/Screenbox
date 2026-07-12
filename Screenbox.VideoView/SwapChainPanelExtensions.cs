using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

[GeneratedComInterface]
[Guid("f92f19d2-3ade-45a6-a20c-f6f1ea90554b")]
internal partial interface ISwapChainPanelNative
{
    void SetSwapChain(IntPtr swapChain);
}

public static class SwapChainPanelExtensions
{
    public static void SetSwapChain(this SwapChainPanel panel, IntPtr swapChainPtr)
    {
        if (panel == null) throw new ArgumentNullException(nameof(panel));

        var winrtObj = panel as WinRT.IWinRTObject;
        if (winrtObj?.NativeObject == null) 
            throw new ObjectDisposedException(nameof(panel), "The underlying WinRT native object has been disposed.");

        IntPtr nativePtr = winrtObj.NativeObject.ThisPtr;
        if (nativePtr == IntPtr.Zero) 
            throw new ObjectDisposedException(nameof(panel), "The underlying WinRT native pointer is null.");

        ComWrappers cw = new StrategyBasedComWrappers();
        var panelNative = (ISwapChainPanelNative)cw.GetOrCreateObjectForComInstance(nativePtr, CreateObjectFlags.None);
        panelNative.SetSwapChain(swapChainPtr);
    }
}
