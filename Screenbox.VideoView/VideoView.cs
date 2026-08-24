using System;
using System.Numerics;
using LibVLCSharp.Shared;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

public sealed class VideoViewInitializedEventArgs : EventArgs
{
    public string[] SwapChainOptions { get; }

    public VideoViewInitializedEventArgs(string[] swapChainOptions) => SwapChainOptions = swapChainOptions;
}

public partial class VideoView : SwapChainPanel
{
    private ID3D11Device? _d3d11Device;
    private ID3D11DeviceContext? _d3d11Context;
    private IDXGISwapChain1? _swapChain;

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
        SizeChanged += (s, e) =>
        {
            if (_loaded)
            {
                UpdateSize();
            }
            else
            {
                CreateSwapChain();
            }
        };
        CompositionScaleChanged += (s, e) =>
        {
            if (_loaded)
            {
                UpdateScale();
            }
        };
        Unloaded += (s, e) => DestroySwapChain();
    }

    private void CreateSwapChain()
    {
        if (ActualHeight == 0 || ActualWidth == 0)
        {
            return;
        }

        DestroySwapChain();

        // 1. Create D3D11 Device and Context
        D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            null!,
            out _d3d11Device,
            out _d3d11Context
        ).CheckError();

        if (_d3d11Device is null || _d3d11Context is null)
        {
            return;
        }

        // 2. Query DXGI Factory from D3D11 Device
        using var dxgiDevice = _d3d11Device.QueryInterface<IDXGIDevice1>();
        dxgiDevice.GetAdapter(out IDXGIAdapter adapter);
        using (adapter)
        {
            using var dxgiFactory = adapter.GetParent<IDXGIFactory2>();

            // 3. Define Swap Chain Description
            SwapChainDescription1 scd = new()
            {
                Width = (uint)(ActualWidth * CompositionScaleX),
                Height = (uint)(ActualHeight * CompositionScaleY),
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SampleDescription(1, 0),
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = 2,
                SwapEffect = SwapEffect.FlipSequential,
                Scaling = Scaling.Stretch,
                AlphaMode = AlphaMode.Unspecified
            };

            // 4. Create Swap Chain for Composition
            _swapChain = dxgiFactory.CreateSwapChainForComposition(_d3d11Device, scd);
        }

        dxgiDevice.MaximumFrameLatency = 1;

        // 5. Set Swap Chain on SwapChainPanel
        this.SetSwapChain(_swapChain.NativePointer);

        _loaded = true;
        UpdateScale();
        UpdateSize();

        // Expose SwapChain options for LibVLC
        var options = new[]
        {
            $"--winrt-d3dcontext=0x{_d3d11Context.NativePointer:x}",
            $"--winrt-swapchain=0x{_swapChain.NativePointer:x}"
        };

        Initialized?.Invoke(this, new VideoViewInitializedEventArgs(options));
    }

    private unsafe void UpdateSize()
    {
        if (!_loaded || _swapChain is null)
        {
            return;
        }

        int w = (int)(ActualWidth * CompositionScaleX);
        int h = (int)(ActualHeight * CompositionScaleY);

        _swapChain.SetPrivateData(SWAPCHAIN_WIDTH, sizeof(int), new IntPtr(&w));
        _swapChain.SetPrivateData(SWAPCHAIN_HEIGHT, sizeof(int), new IntPtr(&h));
    }

    private void UpdateScale()
    {
        if (!_loaded || _swapChain is null)
        {
            return;
        }

        using var swapChain2 = _swapChain.QueryInterface<IDXGISwapChain2>();
        if (swapChain2 is not null)
        {
            var matrix = new Matrix3x2(
                1.0f / (float)CompositionScaleX, 0.0f,
                0.0f, 1.0f / (float)CompositionScaleY,
                0.0f, 0.0f
            );
            swapChain2.MatrixTransform = matrix;
        }
    }

    private void DestroySwapChain()
    {
        if (_loaded)
        {
            try
            {
                this.SetSwapChain(IntPtr.Zero);
            }
            catch (ObjectDisposedException)
            {
                // Safe to ignore ObjectDisposedException during teardown
            }
        }

        _swapChain?.Dispose();
        _d3d11Context?.Dispose();
        _d3d11Device?.Dispose();

        _swapChain = null;
        _d3d11Context = null;
        _d3d11Device = null;
        _loaded = false;
    }
}
