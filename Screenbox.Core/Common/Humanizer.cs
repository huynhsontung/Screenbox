using System;
using System.Globalization;

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

        /// <summary>
        /// Formats a playback rate value for display.
        /// </summary>
        /// <param name="rate">The playback rate to format.</param>
        /// <returns>
        /// A string that represents the playback rate formatted with up to two
        /// decimal places and appended with the multiplication sign (<c>×</c>).
        /// </returns>
        public static string FormatPlaybackRate(double rate)
        {
            return $"{rate.ToString("0.##", CultureInfo.CurrentCulture)}×";
        }
    }
}
