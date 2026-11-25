namespace Screenbox.Core.Enums;

/// <summary>
/// Defines video orientation transforms matching LibVLC's <c>video_orientation_t</c> values
/// accepted by the WinRT Direct3D11 swapchain private data (<c>GUID_SWAPCHAIN_TRANSFORM</c>).
/// </summary>
public enum VideoTransform : uint
{
    /// <summary>
    /// Normal orientation (0°, no flip). Matches <c>ORIENT_NORMAL</c>.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Horizontally flipped (mirrored). Matches <c>ORIENT_HFLIPPED</c>.
    /// </summary>
    FlipHorizontal = 1,

    /// <summary>
    /// Vertically flipped. Matches <c>ORIENT_VFLIPPED</c>.
    /// </summary>
    FlipVertical = 2,

    /// <summary>
    /// Rotated 180°. Matches <c>ORIENT_ROTATED_180</c>.
    /// </summary>
    Rotate180 = 3,

    /// <summary>
    /// Transposed (rotated 90° clockwise, then flipped horizontally). Matches <c>ORIENT_TRANSPOSED</c>.
    /// </summary>
    Transpose = 4,

    /// <summary>
    /// Rotated 270° clockwise (90° counter-clockwise on screen). Matches <c>ORIENT_ROTATED_270</c>.
    /// </summary>
    Rotate270 = 5,

    /// <summary>
    /// Rotated 90° clockwise on screen. Matches <c>ORIENT_ROTATED_90</c>.
    /// </summary>
    Rotate90 = 6,

    /// <summary>
    /// Anti-transposed (rotated 270° clockwise, then flipped horizontally). Matches <c>ORIENT_ANTI_TRANSPOSED</c>.
    /// </summary>
    AntiTranspose = 7
}
