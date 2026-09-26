using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ArkaneSystems.MouseJiggle
{
    internal enum JiggleMode
    {
        Normal,
        Zen
    }

    internal enum ThemeMode
    {
        Dark,
        Light
    }

    internal sealed class PortableSettings
    {
        internal PortableSettings()
        {
            Mode = JiggleMode.Zen;
            IntervalSeconds = 50;
            Theme = ThemeMode.Dark;
        }

        internal JiggleMode Mode { get; set; }
        internal int IntervalSeconds { get; set; }
        internal ThemeMode Theme { get; set; }

        internal static PortableSettings Load(string path, out string warning)
        {
            PortableSettings settings = new PortableSettings();
            warning = null;
            if (!File.Exists(path))
            {
                return settings;
            }

            try
            {
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal) ||
                        line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("[", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    if (string.Equals(key, "Mode", StringComparison.OrdinalIgnoreCase))
                    {
                        JiggleMode mode;
                        if (Enum.TryParse(value, true, out mode))
                        {
                            settings.Mode = mode;
                        }
                        else
                        {
                            warning = "Invalid Mode in MouseJiggle.ini; using Zen.";
                            settings.Mode = JiggleMode.Zen;
                        }
                    }
                    else if (string.Equals(key, "IntervalSeconds", StringComparison.OrdinalIgnoreCase))
                    {
                        int seconds;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds))
                        {
                            settings.IntervalSeconds = Math.Max(1, Math.Min(60, seconds));
                            if (seconds != settings.IntervalSeconds)
                            {
                                warning = "IntervalSeconds was limited to the 1-60 second range.";
                            }
                        }
                        else
                        {
                            warning = "Invalid IntervalSeconds in MouseJiggle.ini; using 50.";
                            settings.IntervalSeconds = 50;
                        }
                    }
                    else if (string.Equals(key, "Theme", StringComparison.OrdinalIgnoreCase))
                    {
                        ThemeMode theme;
                        if (Enum.TryParse(value, true, out theme))
                        {
                            settings.Theme = theme;
                        }
                        else
                        {
                            warning = "Invalid Theme in MouseJiggle.ini; using Dark.";
                            settings.Theme = ThemeMode.Dark;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                warning = "Could not read MouseJiggle.ini: " + exception.Message;
                settings = new PortableSettings();
            }

            return settings;
        }

        internal bool TrySave(string path, out string error)
        {
            string temporaryPath = path + ".tmp";
            error = null;
            try
            {
                string[] lines =
                {
                    "; MouseJiggle portable settings",
                    "[MouseJiggle]",
                    "Mode=" + Mode,
                    "IntervalSeconds=" + IntervalSeconds.ToString(CultureInfo.InvariantCulture),
                    "Theme=" + Theme
                };

                File.WriteAllLines(temporaryPath, lines, new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temporaryPath, path, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(temporaryPath, path, true);
                        File.Delete(temporaryPath);
                    }
                    catch (IOException)
                    {
                        File.Copy(temporaryPath, path, true);
                        File.Delete(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
                return true;
            }
            catch (Exception exception)
            {
                error = "Settings could not be saved: " + exception.Message;
                TryDeleteTemporaryFile(temporaryPath);
                return false;
            }
        }

        private static void TryDeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
