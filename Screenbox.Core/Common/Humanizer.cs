using System;

namespace Screenbox.Core
{
    public static class Humanizer
    {
        public static string ToDuration(double value)
        {
            TimeSpan duration = TimeSpan.FromMilliseconds(value);
            return ToDuration(duration);
        }

        public static string ToDuration(TimeSpan duration)
        {
            long hours = Math.Abs((long)duration.TotalHours);
            return (duration < TimeSpan.Zero ? "-" : string.Empty) + (hours != 0 ? $"{hours}:{duration:mm}:{duration:ss}" : duration.ToString(@"%m\:ss"));
        }
    }
}
