using System;

namespace Screenbox.Core.Models;

/// <summary>
/// Represents raw chapter metadata with timing and title information.
/// </summary>
/// <param name="StartTime">The start timestamp of the chapter.</param>
/// <param name="Duration">The duration of the chapter.</param>
/// <param name="Title">The title or description of the chapter.</param>
public readonly record struct ChapterInfo(TimeSpan StartTime, TimeSpan Duration, string? Title);
