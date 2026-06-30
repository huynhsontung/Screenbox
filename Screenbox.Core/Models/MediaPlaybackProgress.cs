using System;

namespace Screenbox.Core.Models
{
    public record MediaPlaybackProgress(string Location, TimeSpan Position)
    {
        public string Location { get; set; } = Location;
        public TimeSpan Position { get; set; } = Position;

        public MediaPlaybackProgress() : this(string.Empty, TimeSpan.Zero)
        {
        }
    }
}