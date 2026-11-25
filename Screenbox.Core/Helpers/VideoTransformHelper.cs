using Screenbox.Core.Enums;

namespace Screenbox.Core.Helpers;

/// <summary>
/// Provides helper methods for normalizing video rotation angles and composing rotation
/// and flip states into LibVLC <see cref="VideoTransform"/> values.
/// </summary>
public static class VideoTransformHelper
{
    /// <summary>
    /// Normalizes a clockwise rotation angle in degrees to 0, 90, 180, or 270.
    /// Non-right-angle values fall back to 0.
    /// </summary>
    /// <param name="rotationDegrees">The rotation angle in degrees.</param>
    /// <returns>The normalized angle: 0, 90, 180, or 270.</returns>
    public static int NormalizeRotation(int rotationDegrees)
    {
        int normalized = ((rotationDegrees % 360) + 360) % 360;
        return normalized is 0 or 90 or 180 or 270 ? normalized : 0;
    }

    /// <summary>
    /// Returns the next clockwise 90-degree rotation angle.
    /// </summary>
    /// <param name="currentRotationDegrees">The current rotation angle in degrees.</param>
    /// <returns>The new normalized rotation angle in degrees.</returns>
    public static int RotateClockwise(int currentRotationDegrees)
    {
        return (NormalizeRotation(currentRotationDegrees) + 90) % 360;
    }

    /// <summary>
    /// Returns the next counter-clockwise 90-degree rotation angle.
    /// </summary>
    /// <param name="currentRotationDegrees">The current rotation angle in degrees.</param>
    /// <returns>The new normalized rotation angle in degrees.</returns>
    public static int RotateCounterClockwise(int currentRotationDegrees)
    {
        return (NormalizeRotation(currentRotationDegrees) + 270) % 360;
    }

    /// <summary>
    /// Composes a clockwise rotation and horizontal/vertical flip flags into a single
    /// <see cref="VideoTransform"/> value (rotation applied first, then horizontal flip,
    /// then vertical flip).
    /// </summary>
    /// <param name="rotationDegrees">The clockwise rotation angle (0, 90, 180, or 270).</param>
    /// <param name="flipHorizontal">Whether the video is flipped horizontally.</param>
    /// <param name="flipVertical">Whether the video is flipped vertically.</param>
    /// <returns>The composed <see cref="VideoTransform"/> value.</returns>
    public static VideoTransform Compose(int rotationDegrees, bool flipHorizontal, bool flipVertical)
    {
        int effectiveAngle = NormalizeRotation(rotationDegrees);
        if (flipVertical)
        {
            // Vertical flip is equivalent to a 180° rotation followed by a horizontal flip.
            effectiveAngle = (effectiveAngle + 180) % 360;
        }

        bool effectiveHFlip = flipHorizontal ^ flipVertical;

        return (effectiveAngle, effectiveHFlip) switch
        {
            (0, false) => VideoTransform.Normal,
            (0, true) => VideoTransform.FlipHorizontal,
            (90, false) => VideoTransform.Rotate90,
            (90, true) => VideoTransform.Transpose,
            (180, false) => VideoTransform.Rotate180,
            (180, true) => VideoTransform.FlipVertical,
            (270, false) => VideoTransform.Rotate270,
            (270, true) => VideoTransform.AntiTranspose,
            _ => VideoTransform.Normal
        };
    }
}
