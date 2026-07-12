using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.UI.Xaml.Controls;
using Silk.NET.Core.Native;

namespace Screenbox.Controls;

[GeneratedComInterface]
[Guid("f92f19d2-3ade-45a6-a20c-f6f1ea90554b")]
internal partial interface ISwapChainPanelNative
{
    unsafe void SetSwapChain(IUnknown* swapChain);
}

internal static class SwapChainPanelExtensions
{
    internal static unsafe void SetSwapChain(this SwapChainPanel panel, IUnknown* swapChain)
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
        panelNative.SetSwapChain(swapChain);
    }
}
