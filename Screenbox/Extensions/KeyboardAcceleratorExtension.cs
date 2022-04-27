#nullable enable

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
                string modifier = string.Empty;
                if ((ka.Modifiers & VirtualKeyModifiers.Control) != 0)
                {
                    modifier = "Ctrl";
                }
                
                if ((ka.Modifiers & VirtualKeyModifiers.Windows) != 0)
                {
                    modifier += modifier.Length > 0 ? "+Win" : "Win";
                }

                if ((ka.Modifiers & VirtualKeyModifiers.Menu) != 0)
                {
                    modifier += modifier.Length > 0 ? "+Menu" : "Menu";
                }

                if ((ka.Modifiers & VirtualKeyModifiers.Shift) != 0)
                {
                    modifier += modifier.Length > 0 ? "+Shift" : "Shift";
                }

                return modifier.Length > 0 ? $"{modifier}+{ka.Key}" : ka.Key.ToString();
            }

            return ka.Key.ToString();
        }
    }
}
