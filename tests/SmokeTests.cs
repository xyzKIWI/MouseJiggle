using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ArkaneSystems.MouseJiggle
{
    internal static class SmokeTests
    {
        private static int _checks;

        [STAThread]
        private static int Main()
        {
            string temporaryRoot = Path.Combine(Path.GetTempPath(), "MouseJiggleTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRoot);
            try
            {
                TestSettings(temporaryRoot);
                TestLaunchOptions();
                TestStartupManager(temporaryRoot);
                TestJiggler();
                TestMainForm();
                Console.WriteLine("PASS: " + _checks + " checks");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception);
                return 1;
            }
            finally
            {
                try
                {
                    Directory.Delete(temporaryRoot, true);
                }
                catch
                {
                }
            }
        }

        private static void TestSettings(string temporaryRoot)
        {
            string missing = Path.Combine(temporaryRoot, "missing.ini");
            string warning;
            PortableSettings defaults = PortableSettings.Load(missing, out warning);
            Check(defaults.Mode == JiggleMode.Zen, "Missing INI defaults to Zen");
            Check(defaults.IntervalSeconds == 50, "Missing INI defaults to 50 seconds");
            Check(defaults.Theme == ThemeMode.Dark, "Missing INI defaults to Dark theme");
            Check(warning == null, "Missing INI is not an error");

            string valid = Path.Combine(temporaryRoot, "valid.ini");
            PortableSettings saved = new PortableSettings();
            saved.Mode = JiggleMode.Normal;
            saved.IntervalSeconds = 1;
            saved.Theme = ThemeMode.Light;
            string error;
            Check(saved.TrySave(valid, out error), "INI saves successfully");
            PortableSettings reloaded = PortableSettings.Load(valid, out warning);
            Check(reloaded.Mode == JiggleMode.Normal, "INI mode round-trips");
            Check(reloaded.IntervalSeconds == 1, "INI interval round-trips");
            Check(reloaded.Theme == ThemeMode.Light, "INI theme round-trips");

            string invalid = Path.Combine(temporaryRoot, "invalid.ini");
            File.WriteAllText(invalid, "[MouseJiggle]\r\nMode=Unknown\r\nIntervalSeconds=text\r\nTheme=Unknown\r\n");
            PortableSettings recovered = PortableSettings.Load(invalid, out warning);
            Check(recovered.Mode == JiggleMode.Zen, "Invalid mode falls back to Zen");
            Check(recovered.IntervalSeconds == 50, "Invalid interval falls back to 50");
            Check(recovered.Theme == ThemeMode.Dark, "Invalid theme falls back to Dark");
            Check(!string.IsNullOrEmpty(warning), "Invalid INI produces a warning");

            string unavailable = Path.Combine(temporaryRoot, "missing-directory", "settings.ini");
            Check(!saved.TrySave(unavailable, out error), "Unwritable path fails without throwing");
            Check(!string.IsNullOrEmpty(error), "Unwritable path returns an error message");
        }

        private static void TestLaunchOptions()
        {
            LaunchOptions defaults = LaunchOptions.Create(new string[0]);
            Check(defaults.StartJiggling, "Application starts jiggling by default");

            LaunchOptions minimum = LaunchOptions.Create(new[] { "-s:0" });
            Check(minimum.IntervalSeconds == 1, "Command-line interval clamps to 1");

            LaunchOptions maximum = LaunchOptions.Create(new[] { "--seconds:999", "--minimized", "--zen" });
            Check(maximum.IntervalSeconds == 60, "Command-line interval clamps to 60");
            Check(maximum.StartMinimized, "Minimized switch is accepted");
            Check(maximum.ZenMode, "Zen switch is accepted");

            LaunchOptions help = LaunchOptions.Create(new[] { "--help" });
            Check(help.ShowHelp, "Help switch is accepted");
        }

        private static void TestStartupManager(string temporaryRoot)
        {
            string executable = Path.Combine(temporaryRoot, "Folder With Spaces", "MouseJiggle.exe");
            string expected = "\"" + executable + "\" --minimized";
            Check(StartupManager.BuildCommand(executable) == expected, "Startup path is quoted");

            string error;
            try
            {
                Check(StartupManager.TrySetEnabled(executable, true, out error), "Startup entry can be enabled");
                Check(StartupManager.IsEnabled(executable), "Startup entry matches the executable");
            }
            finally
            {
                StartupManager.TrySetEnabled(executable, false, out error);
            }
            Check(!StartupManager.IsEnabled(executable), "Startup entry is removed after the test");
        }

        private static void TestJiggler()
        {
            int errorCode;
            Check(Jiggler.TryJiggle(0, 0, out errorCode), "Zen SendInput succeeds");
            Check(errorCode == 0, "Successful Zen input has no error code");
        }

        private static void TestMainForm()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string executableSettings = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "MouseJiggle.ini");
            File.WriteAllText(executableSettings, "[MouseJiggle]\r\nMode=Zen\r\nIntervalSeconds=50\r\nTheme=Dark\r\n");
            LaunchOptions options = LaunchOptions.Create(new[] { "--zen", "--seconds:50" });
            using (MainForm form = new MainForm(options))
            {
                form.Show();
                Application.DoEvents();
                RadioButton zen = GetField<RadioButton>(form, "_zenMode");
                NumericUpDown interval = GetField<NumericUpDown>(form, "_intervalSeconds");
                Button toggle = GetField<Button>(form, "_toggleButton");
                Label modeDescription = GetField<Label>(form, "_modeDescription");
                ToolStripMenuItem trayToggle = GetField<ToolStripMenuItem>(form, "_trayStartStopItem");
                ToolStripMenuItem trayNormal = GetField<ToolStripMenuItem>(form, "_trayNormalItem");
                ContextMenuStrip trayMenu = GetField<ContextMenuStrip>(form, "_trayMenu");
                CenteredComboBox theme = GetField<CenteredComboBox>(form, "_themeSelector");
                Check(zen.Checked, "Main form starts in Zen mode");
                Check(interval.Value == 50, "Main form starts at 50 seconds");
                Check(GetField<bool>(form, "_isJiggling"), "Main form starts active");
                Check(toggle.Text == "Stop Jiggling", "Active form offers Stop Jiggling");
                Check(theme.SelectedItem.ToString() == "Dark", "Main form starts in Dark theme");
                Check(theme.DrawMode == DrawMode.OwnerDrawFixed, "Theme choices use centered owner drawing");
                Check(AllTopLevelControlsFit(form), "Controls fit at the default scale");
                Check(trayMenu.Items.Count == 7, "Tray menu contains all expected commands and separators");

                toggle.PerformClick();
                Application.DoEvents();
                Check(!GetField<bool>(form, "_isJiggling"), "Toggle pauses jiggling");
                Check(toggle.Text == "Start Jiggling", "Paused form offers Start Jiggling");
                toggle.PerformClick();
                Application.DoEvents();
                Check(GetField<bool>(form, "_isJiggling"), "Toggle resumes jiggling");

                trayToggle.PerformClick();
                Check(!GetField<bool>(form, "_isJiggling"), "Tray menu can pause jiggling");
                trayToggle.PerformClick();
                Check(GetField<bool>(form, "_isJiggling"), "Tray menu can resume jiggling");

                trayNormal.PerformClick();
                Application.DoEvents();
                Check(!zen.Checked, "Tray menu switches to Normal mode");
                Check(modeDescription.Text.StartsWith("Normal", StringComparison.Ordinal), "Mode description follows Normal mode");

                interval.Value = 1;
                Check(interval.Value == 1, "Interval accepts the 1-second boundary");
                interval.Value = 60;
                Check(interval.Value == 60, "Interval accepts the 60-second boundary");

                theme.SelectedItem = "Light";
                Application.DoEvents();
                Check(form.BackColor == System.Drawing.Color.FromArgb(244, 246, 248), "Light theme changes the window palette");
                string savedThemeWarning;
                PortableSettings savedTheme = PortableSettings.Load(executableSettings, out savedThemeWarning);
                Check(savedTheme.Theme == ThemeMode.Light, "Light theme is saved to INI");

                form.Scale(new System.Drawing.SizeF(1.5F, 1.5F));
                Check(AllTopLevelControlsFit(form), "Controls fit after simulated 150 percent scaling");

                form.Close();
                Application.DoEvents();
                NotifyIcon notifyIcon = GetField<NotifyIcon>(form, "_notifyIcon");
                Check(!form.IsDisposed, "Title-bar close keeps the application running");
                Check(!form.Visible && !form.ShowInTaskbar, "Title-bar close hides the window");
                Check(notifyIcon.Visible, "Title-bar close shows the notification-area icon");

                ToolStripMenuItem exit = (ToolStripMenuItem)trayMenu.Items[trayMenu.Items.Count - 1];
                exit.PerformClick();
                Application.DoEvents();
                Check(form.IsDisposed, "Tray Exit closes the application");
            }
        }

        private static bool AllTopLevelControlsFit(Form form)
        {
            bool fits = true;
            foreach (Control control in form.Controls)
            {
                if (control.Visible && !form.ClientRectangle.Contains(control.Bounds))
                {
                    Console.WriteLine(
                        "OUT OF BOUNDS: " + control.GetType().Name + " '" + control.Text +
                        "' bounds=" + control.Bounds + " client=" + form.ClientRectangle);
                    fits = false;
                }
            }
            return fits;
        }

        private static T GetField<T>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("Field not found: " + fieldName);
            }
            return (T)field.GetValue(instance);
        }

        private static void Check(bool condition, string message)
        {
            _checks++;
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
            Console.WriteLine("PASS: " + message);
        }
    }
}
