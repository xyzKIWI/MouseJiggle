// Custom portable user interface based on Mouse Jiggler 1.8.42 (Ms-PL).

using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ArkaneSystems.MouseJiggle
{
    internal sealed class MainForm : Form
    {
        private static readonly Color WindowColor = Color.FromArgb(21, 24, 29);
        private static readonly Color PanelColor = Color.FromArgb(31, 36, 43);
        private static readonly Color BorderColor = Color.FromArgb(58, 66, 77);
        private static readonly Color PrimaryColor = Color.FromArgb(57, 198, 255);
        private static readonly Color MainTextColor = Color.FromArgb(242, 245, 248);
        private static readonly Color MutedTextColor = Color.FromArgb(170, 178, 189);
        private static readonly Color SuccessColor = Color.FromArgb(31, 173, 111);
        private static readonly Color PausedColor = Color.FromArgb(89, 97, 108);
        private static readonly Color ErrorColor = Color.FromArgb(202, 73, 84);
        private static readonly Color LightWindowColor = Color.FromArgb(244, 246, 248);
        private static readonly Color LightPanelColor = Color.White;
        private static readonly Color LightBorderColor = Color.FromArgb(183, 192, 203);
        private static readonly Color LightPrimaryColor = Color.FromArgb(21, 138, 203);
        private static readonly Color LightMainTextColor = Color.FromArgb(23, 32, 42);
        private static readonly Color LightMutedTextColor = Color.FromArgb(85, 97, 111);

        private readonly LaunchOptions _options;
        private readonly Timer _jiggleTimer;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly ToolTip _toolTip;
        private readonly Icon _applicationIcon;

        private Label _statusBadge;
        private Label _messageLabel;
        private Label _modeDescription;
        private Button _toggleButton;
        private RadioButton _normalMode;
        private RadioButton _zenMode;
        private NumericUpDown _intervalSeconds;
        private CenteredComboBox _themeSelector;
        private CheckBox _startWithWindows;
        private ToolStripMenuItem _trayStartStopItem;
        private ToolStripMenuItem _trayNormalItem;
        private ToolStripMenuItem _trayZenItem;

        private bool _initializing;
        private bool _isJiggling;
        private bool _exitRequested;
        private bool _zig = true;
        private string _message;
        private ThemeMode _currentTheme;

        internal MainForm(LaunchOptions options)
        {
            _options = options;
            _initializing = true;
            _message = options.SettingsWarning;
            _currentTheme = options.Theme;
            _applicationIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            _jiggleTimer = new Timer();
            _jiggleTimer.Tick += JiggleTimerTick;

            _toolTip = new ToolTip();
            _trayMenu = BuildTrayMenu();
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = _applicationIcon;
            _notifyIcon.ContextMenuStrip = _trayMenu;
            _notifyIcon.Visible = false;
            _notifyIcon.DoubleClick += delegate { ShowFromTray(); };

            BuildWindow();
            ApplyLaunchOptions();
            _initializing = false;

            if (options.StartJiggling)
            {
                StartJiggling();
            }
            else
            {
                UpdateView();
            }

            Shown += delegate
            {
                if (_options.StartMinimized)
                {
                    MinimizeToTray();
                }
            };
        }

        protected override void WndProc(ref Message message)
        {
            if (Program.ShowWindowMessage != 0 && (uint)message.Msg == Program.ShowWindowMessage)
            {
                ShowFromTray();
                return;
            }
            base.WndProc(ref message);
        }

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            ApplyTitleBarTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _jiggleTimer.Dispose();
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _trayMenu.Dispose();
                _toolTip.Dispose();
                if (_applicationIcon != null)
                {
                    _applicationIcon.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void BuildWindow()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = WindowColor;
            ClientSize = new Size(336, 336);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            ForeColor = MainTextColor;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = _applicationIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MouseJiggle";

            Label title = new Label();
            title.AutoSize = true;
            title.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);
            title.ForeColor = MainTextColor;
            title.Location = new Point(16, 15);
            title.Text = "MouseJiggle";
            Controls.Add(title);

            _statusBadge = new Label();
            _statusBadge.AccessibleName = "Jiggling status";
            _statusBadge.AutoSize = false;
            _statusBadge.Font = new Font(Font.FontFamily, 8F, FontStyle.Bold);
            _statusBadge.ForeColor = Color.White;
            _statusBadge.Location = new Point(244, 14);
            _statusBadge.Size = new Size(76, 24);
            _statusBadge.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_statusBadge);

            _toggleButton = CreateButton(new Point(16, 52), new Size(304, 40), true);
            _toggleButton.AccessibleName = "Start or stop jiggling";
            _toggleButton.Click += delegate
            {
                _message = null;
                if (_isJiggling)
                {
                    StopJiggling();
                }
                else
                {
                    StartJiggling();
                }
            };
            Controls.Add(_toggleButton);

            GroupBox modeGroup = new GroupBox();
            modeGroup.ForeColor = MainTextColor;
            modeGroup.Location = new Point(16, 102);
            modeGroup.Size = new Size(304, 72);
            modeGroup.Text = "Mode";
            Controls.Add(modeGroup);

            _normalMode = CreateRadioButton("Normal", new Point(13, 21));
            _normalMode.AccessibleDescription = "Moves the pointer four pixels back and forth.";
            _normalMode.CheckedChanged += ModeChanged;
            modeGroup.Controls.Add(_normalMode);

            _zenMode = CreateRadioButton("Zen", new Point(116, 21));
            _zenMode.AccessibleDescription = "Sends mouse input without moving the pointer.";
            _zenMode.CheckedChanged += ModeChanged;
            modeGroup.Controls.Add(_zenMode);

            _modeDescription = new Label();
            _modeDescription.AutoSize = true;
            _modeDescription.ForeColor = MutedTextColor;
            _modeDescription.Location = new Point(13, 46);
            modeGroup.Controls.Add(_modeDescription);

            Label intervalLabel = new Label();
            intervalLabel.AutoSize = true;
            intervalLabel.ForeColor = MainTextColor;
            intervalLabel.Location = new Point(16, 189);
            intervalLabel.Text = "Interval (seconds)";
            Controls.Add(intervalLabel);

            _intervalSeconds = new NumericUpDown();
            _intervalSeconds.AccessibleName = "Jiggle interval in seconds";
            _intervalSeconds.BackColor = PanelColor;
            _intervalSeconds.BorderStyle = BorderStyle.FixedSingle;
            _intervalSeconds.ForeColor = MainTextColor;
            _intervalSeconds.Location = new Point(244, 185);
            _intervalSeconds.Maximum = 60;
            _intervalSeconds.Minimum = 1;
            _intervalSeconds.Size = new Size(76, 23);
            _intervalSeconds.TextAlign = HorizontalAlignment.Center;
            _intervalSeconds.ValueChanged += IntervalChanged;
            Controls.Add(_intervalSeconds);

            Label themeLabel = new Label();
            themeLabel.AutoSize = true;
            themeLabel.Location = new Point(16, 219);
            themeLabel.Text = "Window theme";
            Controls.Add(themeLabel);

            _themeSelector = new CenteredComboBox();
            _themeSelector.AccessibleName = "Window theme";
            _themeSelector.FlatStyle = FlatStyle.Flat;
            _themeSelector.Items.Add("Dark");
            _themeSelector.Items.Add("Light");
            _themeSelector.Location = new Point(244, 215);
            _themeSelector.Size = new Size(76, 23);
            _themeSelector.SelectedIndexChanged += ThemeChanged;
            Controls.Add(_themeSelector);

            _startWithWindows = new CheckBox();
            _startWithWindows.AccessibleDescription = "Starts MouseJiggle minimized after signing in to Windows.";
            _startWithWindows.AutoSize = true;
            _startWithWindows.FlatStyle = FlatStyle.Flat;
            _startWithWindows.ForeColor = MainTextColor;
            _startWithWindows.Location = new Point(16, 249);
            _startWithWindows.Text = "Start with Windows (minimized)";
            _startWithWindows.CheckedChanged += StartWithWindowsChanged;
            _toolTip.SetToolTip(_startWithWindows, "Uses the current-user Windows Run setting. No administrator access is required.");
            Controls.Add(_startWithWindows);

            Button trayButton = CreateButton(new Point(16, 277), new Size(143, 28), false);
            trayButton.Text = "Minimize to Tray";
            trayButton.AccessibleDescription = "Hides the window and keeps MouseJiggle running in the notification area.";
            trayButton.Click += delegate { MinimizeToTray(); };
            Controls.Add(trayButton);

            Button aboutButton = CreateButton(new Point(169, 277), new Size(70, 28), false);
            aboutButton.Text = "About";
            aboutButton.Click += delegate { ShowAbout(); };
            Controls.Add(aboutButton);

            Button exitButton = CreateButton(new Point(249, 277), new Size(71, 28), false);
            exitButton.Text = "Exit";
            exitButton.Click += delegate { RequestExit(); };
            Controls.Add(exitButton);

            _messageLabel = new Label();
            _messageLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _messageLabel.AutoEllipsis = true;
            _messageLabel.ForeColor = MutedTextColor;
            _messageLabel.Location = new Point(16, 308);
            _messageLabel.Size = new Size(304, 18);
            _messageLabel.Visible = false;
            Controls.Add(_messageLabel);

            FormClosing += MainFormClosing;

            ResumeLayout(false);
            PerformLayout();
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = PanelColor;
            menu.ForeColor = MainTextColor;
            menu.ShowImageMargin = false;

            _trayStartStopItem = new ToolStripMenuItem("Stop Jiggling");
            _trayStartStopItem.Click += delegate
            {
                if (_isJiggling)
                {
                    StopJiggling();
                }
                else
                {
                    StartJiggling();
                }
            };
            menu.Items.Add(_trayStartStopItem);
            menu.Items.Add(new ToolStripSeparator());

            _trayNormalItem = new ToolStripMenuItem("Normal mode");
            _trayNormalItem.Click += delegate { _normalMode.Checked = true; };
            menu.Items.Add(_trayNormalItem);

            _trayZenItem = new ToolStripMenuItem("Zen mode");
            _trayZenItem.Click += delegate { _zenMode.Checked = true; };
            menu.Items.Add(_trayZenItem);
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem showItem = new ToolStripMenuItem("Show Window");
            showItem.Click += delegate { ShowFromTray(); };
            menu.Items.Add(showItem);

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += delegate { RequestExit(); };
            menu.Items.Add(exitItem);
            return menu;
        }

        private void MainFormClosing(object sender, FormClosingEventArgs eventArgs)
        {
            if (eventArgs.CloseReason == CloseReason.UserClosing && !_exitRequested)
            {
                eventArgs.Cancel = true;
                MinimizeToTray();
                return;
            }

            _jiggleTimer.Stop();
            _notifyIcon.Visible = false;
        }

        private void RequestExit()
        {
            _exitRequested = true;
            Close();
        }

        private void ApplyLaunchOptions()
        {
            _zenMode.Checked = _options.ZenMode;
            _normalMode.Checked = !_options.ZenMode;
            _intervalSeconds.Value = _options.IntervalSeconds;
            _themeSelector.SelectedItem = _options.Theme.ToString();
            _startWithWindows.Checked = StartupManager.IsEnabled(Application.ExecutablePath);
            _jiggleTimer.Interval = _options.IntervalSeconds * 1000;
            ApplyTheme(_options.Theme);
        }

        private Button CreateButton(Point location, Size size, bool primary)
        {
            Button button = new Button();
            button.BackColor = primary ? PrimaryColor : PanelColor;
            button.FlatAppearance.BorderColor = primary ? PrimaryColor : BorderColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font(Font.FontFamily, 9F, primary ? FontStyle.Bold : FontStyle.Regular);
            button.ForeColor = primary ? Color.FromArgb(10, 30, 40) : MainTextColor;
            button.Location = location;
            button.Size = size;
            button.UseVisualStyleBackColor = false;
            return button;
        }

        private RadioButton CreateRadioButton(string text, Point location)
        {
            RadioButton button = new RadioButton();
            button.AutoSize = true;
            button.FlatStyle = FlatStyle.Flat;
            button.ForeColor = MainTextColor;
            button.Location = location;
            button.Text = text;
            return button;
        }

        private void StartJiggling()
        {
            _isJiggling = true;
            ResetJiggleTimer();
            UpdateView();
        }

        private void StopJiggling()
        {
            _isJiggling = false;
            _jiggleTimer.Stop();
            UpdateView();
        }

        private void ResetJiggleTimer()
        {
            _jiggleTimer.Stop();
            _jiggleTimer.Interval = (int)_intervalSeconds.Value * 1000;
            if (_isJiggling)
            {
                _jiggleTimer.Start();
            }
        }

        private void JiggleTimerTick(object sender, EventArgs eventArgs)
        {
            int delta = _zig ? 4 : -4;
            int errorCode;
            bool success = _zenMode.Checked
                ? Jiggler.TryJiggle(0, 0, out errorCode)
                : Jiggler.TryJiggle(delta, delta, out errorCode);

            if (!success)
            {
                _message = "Input failed (Win32 error " + errorCode + "). Jiggling was paused.";
                _isJiggling = false;
                _jiggleTimer.Stop();
                UpdateView(true);
                return;
            }

            _zig = !_zig;
        }

        private void ModeChanged(object sender, EventArgs eventArgs)
        {
            if (_initializing || (!_normalMode.Checked && !_zenMode.Checked))
            {
                return;
            }
            _message = null;
            SavePortableSettings();
            UpdateView();
        }

        private void IntervalChanged(object sender, EventArgs eventArgs)
        {
            if (_initializing)
            {
                return;
            }
            _message = null;
            ResetJiggleTimer();
            SavePortableSettings();
            UpdateView();
        }

        private void ThemeChanged(object sender, EventArgs eventArgs)
        {
            if (_initializing || _themeSelector.SelectedItem == null)
            {
                return;
            }

            ThemeMode theme;
            if (!Enum.TryParse(_themeSelector.SelectedItem.ToString(), true, out theme))
            {
                theme = ThemeMode.Dark;
            }

            _currentTheme = theme;
            _message = null;
            ApplyTheme(theme);
            SavePortableSettings();
            UpdateView();
        }

        private void StartWithWindowsChanged(object sender, EventArgs eventArgs)
        {
            if (_initializing)
            {
                return;
            }

            string error;
            if (!StartupManager.TrySetEnabled(Application.ExecutablePath, _startWithWindows.Checked, out error))
            {
                _message = error;
                _initializing = true;
                _startWithWindows.Checked = StartupManager.IsEnabled(Application.ExecutablePath);
                _initializing = false;
            }
            else
            {
                _message = _startWithWindows.Checked
                    ? "Windows startup enabled for this file location."
                    : "Windows startup disabled.";
            }
            UpdateView();
        }

        private void SavePortableSettings()
        {
            PortableSettings settings = new PortableSettings();
            settings.Mode = _zenMode.Checked ? JiggleMode.Zen : JiggleMode.Normal;
            settings.IntervalSeconds = (int)_intervalSeconds.Value;
            settings.Theme = _currentTheme;
            string error;
            if (!settings.TrySave(_options.SettingsPath, out error))
            {
                _message = error;
            }
        }

        private void ApplyTheme(ThemeMode theme)
        {
            _currentTheme = theme;
            bool dark = theme == ThemeMode.Dark;
            Color window = dark ? WindowColor : LightWindowColor;
            Color panel = dark ? PanelColor : LightPanelColor;
            Color border = dark ? BorderColor : LightBorderColor;
            Color primary = dark ? PrimaryColor : LightPrimaryColor;
            Color text = dark ? MainTextColor : LightMainTextColor;
            Color muted = dark ? MutedTextColor : LightMutedTextColor;

            BackColor = window;
            ForeColor = text;
            foreach (Control control in Controls)
            {
                ApplyControlTheme(control, window, panel, border, primary, text, muted, dark);
            }

            _trayMenu.BackColor = panel;
            _trayMenu.ForeColor = text;
            foreach (ToolStripItem item in _trayMenu.Items)
            {
                item.BackColor = panel;
                item.ForeColor = text;
            }

            ApplyTitleBarTheme();
            Invalidate(true);
        }

        private void ApplyControlTheme(
            Control control,
            Color window,
            Color panel,
            Color border,
            Color primary,
            Color text,
            Color muted,
            bool dark)
        {
            if (control == _statusBadge)
            {
                return;
            }

            Button button = control as Button;
            if (button != null)
            {
                bool isPrimary = button == _toggleButton;
                button.BackColor = isPrimary ? primary : panel;
                button.ForeColor = isPrimary ? (dark ? Color.FromArgb(10, 30, 40) : Color.White) : text;
                button.FlatAppearance.BorderColor = isPrimary ? primary : border;
                return;
            }

            if (control is NumericUpDown || control is ComboBox)
            {
                control.BackColor = panel;
                control.ForeColor = text;
                return;
            }

            control.BackColor = window;
            control.ForeColor = control == _messageLabel || control == _modeDescription ? muted : text;
            foreach (Control child in control.Controls)
            {
                ApplyControlTheme(child, window, panel, border, primary, text, muted, dark);
            }
        }

        private void ApplyTitleBarTheme()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            bool dark = _currentTheme == ThemeMode.Dark;
            WindowThemeManager.Apply(
                Handle,
                _currentTheme,
                dark ? WindowColor : LightWindowColor,
                dark ? MainTextColor : LightMainTextColor,
                dark ? BorderColor : LightBorderColor);
        }

        private void UpdateView()
        {
            UpdateView(false);
        }

        private void UpdateView(bool error)
        {
            if (error)
            {
                _statusBadge.Text = "ERROR";
                _statusBadge.BackColor = ErrorColor;
            }
            else if (_isJiggling)
            {
                _statusBadge.Text = "ACTIVE";
                _statusBadge.BackColor = SuccessColor;
            }
            else
            {
                _statusBadge.Text = "PAUSED";
                _statusBadge.BackColor = PausedColor;
            }

            _toggleButton.Text = _isJiggling ? "Stop Jiggling" : "Start Jiggling";
            _trayStartStopItem.Text = _toggleButton.Text;
            _trayNormalItem.Checked = _normalMode.Checked;
            _trayZenItem.Checked = _zenMode.Checked;

            string mode = _zenMode.Checked ? "Zen" : "Normal";
            _modeDescription.Text = _zenMode.Checked
                ? "Zen prevents idle without moving the pointer."
                : "Normal moves the pointer 4 px back and forth.";
            _notifyIcon.Text = _isJiggling
                ? "MouseJiggle active - " + mode + ", " + _intervalSeconds.Value + "s"
                : "MouseJiggle paused";

            string detail = _message;
            if (string.IsNullOrEmpty(detail))
            {
                detail = _isJiggling
                    ? mode + " mode every " + _intervalSeconds.Value + " seconds."
                    : "Jiggling is stopped.";
            }
            _messageLabel.Text = detail;
            _messageLabel.Visible = true;
        }

        private void MinimizeToTray()
        {
            _notifyIcon.Visible = true;
            Hide();
            ShowInTaskbar = false;
        }

        private void ShowFromTray()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(ShowFromTray));
                return;
            }

            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
            _notifyIcon.Visible = false;
        }

        private void ShowAbout()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            MessageBox.Show(
                "MouseJiggle " + version + "\r\n\r\n" +
                "Custom portable build based on Mouse Jiggler 1.8.42 by Arkane Systems.\r\n" +
                "Original and modified source are licensed under the Microsoft Public License (Ms-PL).\r\n\r\n" +
                "Use only where permitted by your organization. This application does not hide its activity.",
                "About MouseJiggle",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
