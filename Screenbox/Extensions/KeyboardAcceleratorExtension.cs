#nullable enable

using System.Text;
using Windows.System;
using Windows.UI.Xaml.Input;

namespace Screenbox.Extensions
{
    internal static class KeyboardAcceleratorExtension
    {
        public static string ToShortcut(this KeyboardAccelerator? ka)
        {
            if (ka == null) return string.Empty;
            if (ka.Modifiers != VirtualKeyModifiers.None)
            {
                StringBuilder builder = new(16);
                if ((ka.Modifiers & VirtualKeyModifiers.Control) != 0)
                {
                    builder.Append("Ctrl+");
                }
                
                if ((ka.Modifiers & VirtualKeyModifiers.Windows) != 0)
                {
                    builder.Append(builder.Length > 0 ? "+Win+" : "Win+");
                }

                if ((ka.Modifiers & VirtualKeyModifiers.Menu) != 0)
                {
                    builder.Append(builder.Length > 0 ? "+Menu+" : "Menu+");
                }

                if ((ka.Modifiers & VirtualKeyModifiers.Shift) != 0)
                {
                    builder.Append(builder.Length > 0 ? "+Shift+" : "Shift+");
                }

                return builder.Append(ka.Key.ToString()).ToString();
            }

            return ka.Key.ToString();
        }
    }
}
