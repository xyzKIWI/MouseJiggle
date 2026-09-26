using System;
using Microsoft.Win32;

namespace ArkaneSystems.MouseJiggle
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "MouseJiggle";

        internal static bool IsEnabled(string executablePath)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    string value = key == null ? null : key.GetValue(ValueName) as string;
                    return string.Equals(value, BuildCommand(executablePath), StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        internal static bool TrySetEnabled(string executablePath, bool enabled, out string error)
        {
            error = null;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null)
                    {
                        error = "The Windows startup setting could not be opened.";
                        return false;
                    }

                    if (enabled)
                    {
                        key.SetValue(ValueName, BuildCommand(executablePath), RegistryValueKind.String);
                    }
                    else
                    {
                        string existing = key.GetValue(ValueName) as string;
                        if (string.Equals(existing, BuildCommand(executablePath), StringComparison.OrdinalIgnoreCase))
                        {
                            key.DeleteValue(ValueName, false);
                        }
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                error = "The Windows startup setting could not be changed: " + exception.Message;
                return false;
            }
        }

        internal static string BuildCommand(string executablePath)
        {
            return "\"" + executablePath + "\" --minimized";
        }
    }
}
