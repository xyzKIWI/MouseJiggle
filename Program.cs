using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ArkaneSystems.MouseJiggle
{
    internal static class Program
    {
        internal const string SingleInstanceMutexName = "Local\\MouseJiggle.Custom.1.8.42";
        internal const string ShowWindowMessageName = "MouseJiggle.Custom.ShowWindow.1.8.42";
        private const int AttachParentProcess = -1;

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string messageName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr windowHandle, uint message, IntPtr wordParameter, IntPtr longParameter);

        internal static uint ShowWindowMessage { get; private set; }

        [STAThread]
        private static void Main(string[] args)
        {
            LaunchOptions options = LaunchOptions.Create(args);
            if (options.ShowHelp)
            {
                WriteHelpInfo();
                return;
            }

            bool createdNew;
            using (Mutex instance = new Mutex(true, SingleInstanceMutexName, out createdNew))
            {
                ShowWindowMessage = RegisterWindowMessage(ShowWindowMessageName);
                if (!createdNew)
                {
                    if (ShowWindowMessage != 0)
                    {
                        PostMessage(new IntPtr(0xffff), ShowWindowMessage, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs eventArgs)
                {
                    MessageBox.Show(eventArgs.Exception.Message, "MouseJiggle error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                using (MainForm form = new MainForm(options))
                {
                    Application.Run(form);
                }
            }
        }

        private static void WriteHelpInfo()
        {
            AttachConsole(AttachParentProcess);
            Console.WriteLine();
            Console.WriteLine("MouseJiggle 1.8.42 custom portable build");
            Console.WriteLine("Usage:");
            Console.WriteLine("  -j or --jiggle      Start with jiggling enabled (enabled by default)");
            Console.WriteLine("  -z or --zen         Start in Zen mode");
            Console.WriteLine("  -m or --minimized   Start minimized to the notification area");
            Console.WriteLine("  -s:X or --seconds:X Set the interval from 1 to 60 seconds");
            Console.WriteLine("  -h or --help        Show this help information");
        }
    }

    internal sealed class LaunchOptions
    {
        private LaunchOptions()
        {
        }

        internal bool StartJiggling { get; private set; }
        internal bool ZenMode { get; private set; }
        internal bool StartMinimized { get; private set; }
        internal int IntervalSeconds { get; private set; }
        internal ThemeMode Theme { get; private set; }
        internal bool ShowHelp { get; private set; }
        internal string SettingsPath { get; private set; }
        internal string SettingsWarning { get; private set; }

        internal static LaunchOptions Create(string[] args)
        {
            string executableDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            string settingsPath = Path.Combine(executableDirectory, "MouseJiggle.ini");
            string warning;
            PortableSettings settings = PortableSettings.Load(settingsPath, out warning);

            LaunchOptions result = new LaunchOptions();
            result.StartJiggling = true;
            result.ZenMode = settings.Mode == JiggleMode.Zen;
            result.IntervalSeconds = settings.IntervalSeconds;
            result.Theme = settings.Theme;
            result.SettingsPath = settingsPath;
            result.SettingsWarning = warning;

            foreach (string rawArgument in args)
            {
                string argument = rawArgument.Trim();
                if (EqualsAny(argument, "-h", "--help"))
                {
                    result.ShowHelp = true;
                }
                else if (EqualsAny(argument, "-j", "--jiggle"))
                {
                    result.StartJiggling = true;
                }
                else if (EqualsAny(argument, "-z", "--zen"))
                {
                    result.ZenMode = true;
                }
                else if (EqualsAny(argument, "-m", "--minimized"))
                {
                    result.StartMinimized = true;
                }
                else if (argument.StartsWith("-s:", StringComparison.OrdinalIgnoreCase) ||
                         argument.StartsWith("--seconds:", StringComparison.OrdinalIgnoreCase))
                {
                    int separator = argument.IndexOf(':');
                    int seconds;
                    if (separator >= 0 && int.TryParse(argument.Substring(separator + 1), out seconds))
                    {
                        result.IntervalSeconds = Math.Max(1, Math.Min(60, seconds));
                    }
                }
            }

            return result;
        }

        private static bool EqualsAny(string value, params string[] choices)
        {
            return choices.Any(choice => string.Equals(value, choice, StringComparison.OrdinalIgnoreCase));
        }
    }
}
