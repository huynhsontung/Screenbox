using System.Runtime.InteropServices;
using Windows.System;

namespace Screenbox.Core.Tests.Helpers;

public static partial class DispatcherQueueTestHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {
        public int dwSize;
        public int threadType;
        public int apartmentType;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nuint wParam;
        public nint lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
        public uint lPrivate;
    }

    [LibraryImport("CoreMessaging.dll")]
    private static partial int CreateDispatcherQueueController(
        DispatcherQueueOptions options,
        out nint dispatcherQueueController);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PeekMessageW(
        out MSG lpMsg,
        nint hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax,
        uint wRemoveMsg);

    [LibraryImport("user32.dll")]
    private static partial nint DispatchMessageW(in MSG lpMsg);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TranslateMessage(in MSG lpMsg);

    public static void EnsureDispatcherQueue()
    {
        if (DispatcherQueue.GetForCurrentThread() is not null)
        {
            return;
        }

        var options = new DispatcherQueueOptions
        {
            dwSize = Marshal.SizeOf<DispatcherQueueOptions>(),
            threadType = 2, // DQTYPE_THREAD_CURRENT
            apartmentType = 2 // DQTAT_COM_STA
        };

        var hr = CreateDispatcherQueueController(options, out _);
        Marshal.ThrowExceptionForHR(hr);

        var queue = DispatcherQueue.GetForCurrentThread();
        if (queue is not null)
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(queue));
        }
    }

    public static void PumpEvents()
    {
        while (PeekMessageW(out var msg, nint.Zero, 0, 0, 1)) // PM_REMOVE = 1
        {
            TranslateMessage(in msg);
            DispatchMessageW(in msg);
        }
    }
}
