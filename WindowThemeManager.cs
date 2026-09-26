using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ArkaneSystems.MouseJiggle
{
    internal static class WindowThemeManager
    {
        private const int UseImmersiveDarkModeBefore20H1 = 19;
        private const int UseImmersiveDarkMode = 20;
        private const int BorderColor = 34;
        private const int CaptionColor = 35;
        private const int TextColor = 36;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int valueSize);

        internal static void Apply(IntPtr windowHandle, ThemeMode theme, Color caption, Color text, Color border)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                int dark = theme == ThemeMode.Dark ? 1 : 0;
                int result = DwmSetWindowAttribute(windowHandle, UseImmersiveDarkMode, ref dark, sizeof(int));
                if (result != 0)
                {
                    DwmSetWindowAttribute(windowHandle, UseImmersiveDarkModeBefore20H1, ref dark, sizeof(int));
                }

                int captionColor = ToColorReference(caption);
                int textColor = ToColorReference(text);
                int borderColor = ToColorReference(border);
                DwmSetWindowAttribute(windowHandle, CaptionColor, ref captionColor, sizeof(int));
                DwmSetWindowAttribute(windowHandle, TextColor, ref textColor, sizeof(int));
                DwmSetWindowAttribute(windowHandle, BorderColor, ref borderColor, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        private static int ToColorReference(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }
    }
}
