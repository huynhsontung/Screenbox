using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using SharpGen.Runtime;
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
    // Maximum 2D texture width and height for Direct3D 11 Feature Level 11_0 and 11_1
    // (D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION = 16384, defined in d3d11.h and the Direct3D 11
    // hardware resource limits specification). Swap chain composition buffers are 2D textures
    // and cannot exceed this limit.
    private const int MaxTextureDimension = 16384;

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
        if (!double.IsFinite(ActualWidth) || !double.IsFinite(ActualHeight) ||
            ActualWidth <= 0 || ActualHeight <= 0 ||
            !double.IsFinite(CompositionScaleX) || !double.IsFinite(CompositionScaleY) ||
            CompositionScaleX <= 0 || CompositionScaleY <= 0)
        {
            return;
        }

        uint width = (uint)Math.Clamp(Math.Round(ActualWidth * CompositionScaleX), 1.0, MaxTextureDimension);
        uint height = (uint)Math.Clamp(Math.Round(ActualHeight * CompositionScaleY), 1.0, MaxTextureDimension);

        DestroySwapChain();

        // Attempt hardware device creation first; fall back to WARP (software rasterizer)
        // if hardware initialization fails (e.g. out of video memory, driver crash, or device removed).
        if (!TryInitializeSwapChain(DriverType.Hardware, width, height) &&
            !TryInitializeSwapChain(DriverType.Warp, width, height))
        {
            DestroySwapChain();
        }
    }

    private bool TryInitializeSwapChain(DriverType driverType, uint width, uint height)
    {
        try
        {
            Result result = D3D11.D3D11CreateDevice(
                null,
                driverType,
                DeviceCreationFlags.BgraSupport,
                null!,
                out _d3d11Device,
                out _d3d11Context);

            if (!result.Success || _d3d11Device is null || _d3d11Context is null)
            {
                CleanUpDevice();
                return false;
            }

            using var dxgiDevice = _d3d11Device.QueryInterface<IDXGIDevice1>();
            dxgiDevice.GetAdapter(out IDXGIAdapter adapter);
            using (adapter)
            {
                using var dxgiFactory = adapter.GetParent<IDXGIFactory2>();

                SwapChainDescription1 scd = new()
                {
                    Width = width,
                    Height = height,
                    Format = Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new SampleDescription(1, 0),
                    BufferUsage = Usage.RenderTargetOutput,
                    BufferCount = 2,
                    SwapEffect = SwapEffect.FlipSequential,
                    Scaling = Scaling.Stretch,
                    AlphaMode = AlphaMode.Unspecified
                };

                _swapChain = dxgiFactory.CreateSwapChainForComposition(_d3d11Device, scd);
            }

            dxgiDevice.MaximumFrameLatency = 1;

            this.SetSwapChain(_swapChain.NativePointer);

            _loaded = true;
            UpdateScale();
            UpdateSize();

            string[] options =
            [
                $"--winrt-d3dcontext=0x{_d3d11Context.NativePointer:x}",
                $"--winrt-swapchain=0x{_swapChain.NativePointer:x}"
            ];

            Initialized?.Invoke(this, new VideoViewInitializedEventArgs(options));
            return true;
        }
        catch (Exception ex) when (ex is ObjectDisposedException || IsGraphicsException(ex))
        {
            CleanUpDevice();
            return false;
        }
    }

    private unsafe void UpdateSize()
    {
        if (!_loaded || _swapChain is null)
        {
            return;
        }

        if (!double.IsFinite(ActualWidth) || !double.IsFinite(ActualHeight) ||
            ActualWidth <= 0 || ActualHeight <= 0 ||
            !double.IsFinite(CompositionScaleX) || !double.IsFinite(CompositionScaleY) ||
            CompositionScaleX <= 0 || CompositionScaleY <= 0)
        {
            return;
        }

        int w = (int)Math.Clamp(Math.Round(ActualWidth * CompositionScaleX), 1.0, MaxTextureDimension);
        int h = (int)Math.Clamp(Math.Round(ActualHeight * CompositionScaleY), 1.0, MaxTextureDimension);

        try
        {
            _swapChain.SetPrivateData(SWAPCHAIN_WIDTH, sizeof(int), new IntPtr(&w));
            _swapChain.SetPrivateData(SWAPCHAIN_HEIGHT, sizeof(int), new IntPtr(&h));
        }
        catch (Exception ex) when (IsGraphicsException(ex))
        {
            // Safe to ignore metadata update failures if the device is unavailable.
        }
    }

    private void UpdateScale()
    {
        if (!_loaded || _swapChain is null)
        {
            return;
        }

        if (!double.IsFinite(CompositionScaleX) || !double.IsFinite(CompositionScaleY) ||
            CompositionScaleX <= 0 || CompositionScaleY <= 0)
        {
            return;
        }

        try
        {
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
        catch (Exception ex) when (IsGraphicsException(ex))
        {
            // Safe to ignore transform failures if the device is unavailable.
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
            catch (Exception ex) when (ex is ObjectDisposedException || IsGraphicsException(ex))
            {
                // Safe to ignore teardown failures after the graphics device is unavailable.
            }
        }

        CleanUpDevice();
    }

    private void CleanUpDevice()
    {
        _swapChain?.Dispose();
        _d3d11Context?.Dispose();
        _d3d11Device?.Dispose();

        _swapChain = null;
        _d3d11Context = null;
        _d3d11Device = null;
        _loaded = false;
    }

    private static bool IsGraphicsException(Exception ex)
    {
        return ex is SharpGenException or COMException;
    }
}
