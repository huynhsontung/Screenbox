using System;

namespace Screenbox.Converters;

/// <summary>
/// Defines constants that specify the filter type for a <see cref="ThicknessFiltersConverter"/> instance.
/// <para>This enumeration supports a bitwise combination of its member values.</para>
/// </summary>
/// <remarks>This enumeration is used by the <see cref="ThicknessFiltersConverter.Filters"/> property.</remarks>
[Flags]
public enum ThicknessFilterKinds
{
    /// <summary>
    /// No filter applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Filters Left value, sets Top, Right and Bottom to 0.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Filters Top value, sets Left, Right and Bottom to 0.
    /// </summary>
    Top = 2,

    /// <summary>
    /// Filters Right value, sets Left, Top and Bottom to 0.
    /// </summary>
    Right = 4,

    /// <summary>
    /// Filters Bottom value, sets Left, Top and Right to 0.
    /// </summary>
    Bottom = 8,

    /// <summary>
    /// Filters Left and Right values, sets Top and Bottom to 0.
    /// </summary>
    /// <remarks>This value combines the <see cref="Left"/> and <see cref="Right"/> flags.</remarks>
    Horizontal = Left | Right,

    /// <summary>
    /// Filters Top and Bottom values, sets Left and Right to 0.
    /// </summary>
    /// <remarks>This value combines the <see cref="Top"/> and <see cref="Bottom"/> flags.</remarks>
    Vertical = Top | Bottom,

    /// <summary>
    /// Filters Left, Top, Right, and Bottom values.
    /// </summary>
    /// <remarks>This value combines the <see cref="Left"/>, <see cref="Top"/>, <see cref="Right"/>, and <see cref="Bottom"/> flags.</remarks>
    All = Left | Top | Right | Bottom
}
