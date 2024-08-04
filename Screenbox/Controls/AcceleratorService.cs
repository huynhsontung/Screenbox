#nullable enable

using Screenbox.Core.Helpers;
using System.Linq;
using System.Text;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Controls
{
    public sealed class AcceleratorService : DependencyObject
    {
        public static readonly DependencyProperty ToolTipProperty =
            DependencyProperty.RegisterAttached(
                "ToolTip",
                typeof(string),
                typeof(AcceleratorService),
                new PropertyMetadata(string.Empty)
            );

        public static void SetToolTip(UIElement element, string value)
        {
            element.SetValue(ToolTipProperty, value);
            KeyboardAccelerator? accelerator = element.KeyboardAccelerators.FirstOrDefault(x => x.IsEnabled);
            bool shouldShowShortcut = SystemInformation.IsDesktop;
            if (accelerator != null && shouldShowShortcut)
            {
                string shortcut = ToShortcut(accelerator);
                ToolTipService.SetToolTip(element,
                    string.IsNullOrEmpty(shortcut) ? value :
                    App.IsRightToLeftLanguage ? $"({shortcut}) {value}" : $"{value} ({shortcut})");
            }
            else
            {
                ToolTipService.SetToolTip(element, value);
            }
        }

        public static string GetToolTip(UIElement element)
        {
            return (string)element.GetValue(ToolTipProperty);
        }

        private static string ToShortcut(KeyboardAccelerator ka)
        {
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
                    builder.Append(builder.Length > 0 ? "+Alt+" : "Alt+");
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
