#nullable enable

using System;
using Windows.UI.Xaml.Data;
using CommunityToolkit.Diagnostics;
using Screenbox.ViewModels;

namespace Screenbox.Converters
{
    internal class ToggleButtonCheckToRepeatModeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            Guard.IsOfType<RepeatMode>(value, nameof(value));
            RepeatMode repeatMode = (RepeatMode)value;
            switch (repeatMode)
            {
                case RepeatMode.Off:
                    return false;
                case RepeatMode.All:
                    return true;
                case RepeatMode.One:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object? value, Type targetType, object parameter, string language)
        {
            if (value == null) return RepeatMode.One;
            bool check = (bool)value;
            return check ? RepeatMode.All : RepeatMode.Off;
        }
    }
}
