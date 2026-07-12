using System;
using LibVLCSharp.Shared;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

public sealed class VideoViewInitializedEventArgs : EventArgs
{
    public string[] SwapChainOptions { get; }
    public VideoViewInitializedEventArgs(string[] swapChainOptions) => SwapChainOptions = swapChainOptions;
}

public unsafe partial class VideoView : SwapChainPanel
{
    private D3D11 _d3d11;
    private DXGI _dxgi;

    private ComPtr<ID3D11Device> _d3d11Device;
    private ComPtr<ID3D11DeviceContext> _d3d11Context;
    private ComPtr<IDXGISwapChain1> _swapChain;

    private bool _loaded;
    private static readonly Guid SWAPCHAIN_WIDTH = new("f1b59347-1643-411a-ad6b-c780177a06b6");
    private static readonly Guid SWAPCHAIN_HEIGHT = new("6ea976a0-9d60-4bb7-a5a9-7dd1187fc9bd");

    public event EventHandler<VideoViewInitializedEventArgs>? Initialized;

    public static readonly DependencyProperty MediaPlayerProperty = DependencyProperty.Register(
        nameof(MediaPlayer), typeof(MediaPlayer), typeof(VideoView), new PropertyMetadata(null));

    public MediaPlayer? MediaPlayer
    {
        get => (MediaPlayer?)GetValue(MediaPlayerProperty);
        set => SetValue(MediaPlayerProperty, value);
    }

    public VideoView()
    {
        _d3d11 = new D3D11(new DefaultNativeContext("d3d11"));
        _dxgi = new DXGI(new DefaultNativeContext("dxgi"));

        SizeChanged += (s, e) =>
        {
            if (_loaded) UpdateSize();
            else CreateSwapChain();
        };
        CompositionScaleChanged += (s, e) =>
        {
            if (_loaded) UpdateScale();
        };
        Unloaded += (s, e) => DestroySwapChain();
    }

    private void CreateSwapChain()
    {
        if (ActualHeight == 0 || ActualWidth == 0) return;

        DestroySwapChain();

        // 1. Create D3D11 Device and Context (pass null to use default feature levels)
        _d3d11.CreateDevice(
            default(ComPtr<IDXGIAdapter>),
            D3DDriverType.Hardware,
            IntPtr.Zero,
            (uint)CreateDeviceFlag.BgraSupport,
            null,
            0,
            D3D11.SdkVersion,
            ref _d3d11Device,
            null,
            ref _d3d11Context
        );

        // 2. Query DXGI Factory from D3D11 Device
        using var dxgiDevice = _d3d11Device.QueryInterface<IDXGIDevice1>();
        ComPtr<IDXGIAdapter> dxgiAdapter = default;
        try
        {
            dxgiDevice.GetAdapter(ref dxgiAdapter);
            using var dxgiAdapter1 = dxgiAdapter.QueryInterface<IDXGIAdapter1>();
            using var dxgiFactory = dxgiAdapter1.GetParent<IDXGIFactory2>();

            // 3. Define Swap Chain Description
            SwapChainDesc1 scd = new()
            {
                Width = (uint)(ActualWidth * CompositionScaleX),
                Height = (uint)(ActualHeight * CompositionScaleY),
                Format = Format.FormatB8G8R8A8Unorm,
                Stereo = false,
                SampleDesc = new SampleDesc(1, 0),
                BufferUsage = DXGI.UsageRenderTargetOutput,
                BufferCount = 2,
                SwapEffect = SwapEffect.FlipSequential,
                Scaling = Scaling.Stretch,
                AlphaMode = AlphaMode.Unspecified
            };

            // 4. Create Swap Chain for Composition (using Handle to bypass extension method ambiguities)
            IDXGISwapChain1* swapChainPtr = null;
            int hr = dxgiFactory.Handle->CreateSwapChainForComposition(
                (IUnknown*)_d3d11Device.Handle,
                &scd,
                null,
                &swapChainPtr
            );
            SilkMarshal.ThrowHResult(hr);
            _swapChain = new ComPtr<IDXGISwapChain1>(swapChainPtr);
        }
        finally
        {
            dxgiAdapter.Dispose();
        }

        dxgiDevice.SetMaximumFrameLatency(1);

        // 5. Set Swap Chain on SwapChainPanel
        this.SetSwapChain((IntPtr)_swapChain.Handle);

        _loaded = true;
        UpdateScale();
        UpdateSize();

        // Expose SwapChain options for LibVLC
        var options = new[]
        {
            $"--winrt-d3dcontext=0x{(IntPtr)_d3d11Context.Handle:x}",
            $"--winrt-swapchain=0x{(IntPtr)_swapChain.Handle:x}"
        };

        Initialized?.Invoke(this, new VideoViewInitializedEventArgs(options));
    }

    private void UpdateSize()
    {
        if (!_loaded || _swapChain.Handle == null) return;

        int w = (int)(ActualWidth * CompositionScaleX);
        int h = (int)(ActualHeight * CompositionScaleY);

        Guid widthGuid = SWAPCHAIN_WIDTH;
        Guid heightGuid = SWAPCHAIN_HEIGHT;

        _swapChain.SetPrivateData(&widthGuid, sizeof(int), &w);
        _swapChain.SetPrivateData(&heightGuid, sizeof(int), &h);
    }

    private void UpdateScale()
    {
        if (!_loaded || _swapChain.Handle == null) return;

        using var swapChain2 = _swapChain.QueryInterface<IDXGISwapChain2>();
        if (swapChain2.Handle != null)
        {
            var matrix = new Silk.NET.DXGI.Matrix3X2F(
                1.0f / CompositionScaleX, 0.0f,
                0.0f, 1.0f / CompositionScaleY,
                0.0f, 0.0f
            );
            swapChain2.Handle->SetMatrixTransform(&matrix);
        }
    }

    private void DestroySwapChain()
    {
        this.SetSwapChain(IntPtr.Zero);

        _swapChain.Dispose();
        _d3d11Context.Dispose();
        _d3d11Device.Dispose();
        _loaded = false;
    }
}
