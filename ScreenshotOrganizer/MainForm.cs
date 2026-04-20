namespace ScreenshotOrganizer;

public class MainForm : Form
{
    private AppSettings _settings;
    private NotifyIcon _trayIcon = null!;

    private Label _folderValueLabel = null!;
    private Label _taskStatusLabel = null!;
    private Button _organizeBtn = null!;
    private Button _settingsBtn = null!;
    private RichTextBox _logBox = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    private bool _closingToTray = true;

    public MainForm()
    {
        _settings = AppSettings.Load();
        InitializeComponents();
        InitializeTray();
        RefreshStatus();
        LoadFileLog();
    }

    // -------------------------------------------------------------------------
    // UI Setup
    // -------------------------------------------------------------------------

    private void InitializeComponents()
    {
        Text = "Screenshot Organizer";
        Size = new Size(660, 540);
        MinimumSize = new Size(500, 420);
        StartPosition = FormStartPosition.CenterScreen;

        // --- Top info panel ---
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(12, 8, 12, 4) };

        var folderCaption = new Label
        {
            Text = "Überwachter Ordner:",
            AutoSize = true,
            Location = new Point(12, 8),
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold)
        };
        _folderValueLabel = new Label
        {
            AutoSize = false,
            Location = new Point(12, 28),
            Size = new Size(620, 18),
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = new Font("Consolas", 8.5f)
        };
        _taskStatusLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 52),
            Font = new Font(Font.FontFamily, 8f)
        };
        topPanel.Controls.AddRange(new Control[] { folderCaption, _folderValueLabel, _taskStatusLabel });

        // --- Button panel ---
        var btnPanel = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 8, 12, 0) };

        _organizeBtn = new Button
        {
            Text = "▶  Jetzt organisieren",
            Size = new Size(170, 32),
            Location = new Point(12, 8),
            BackColor = Color.FromArgb(0, 102, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _organizeBtn.FlatAppearance.BorderSize = 0;
        _organizeBtn.Click += OrganizeBtn_Click;

        _settingsBtn = new Button
        {
            Text = "⚙  Einstellungen",
            Size = new Size(148, 32),
            Location = new Point(190, 8),
            FlatStyle = FlatStyle.System
        };
        _settingsBtn.Click += SettingsBtn_Click;

        var clearBtn = new Button
        {
            Text = "Log leeren",
            Size = new Size(100, 32),
            Location = new Point(346, 8),
            FlatStyle = FlatStyle.System
        };
        clearBtn.Click += (_, _) => _logBox.Clear();

        btnPanel.Controls.AddRange(new Control[] { _organizeBtn, _settingsBtn, clearBtn });

        // --- Log area ---
        _logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 4, 12, 4) };
        logPanel.Controls.Add(_logBox);

        // --- Status bar ---
        var statusStrip = new StatusStrip { SizingGrip = false };
        _statusLabel = new ToolStripStatusLabel("Bereit") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        statusStrip.Items.Add(_statusLabel);

        Controls.Add(logPanel);
        Controls.Add(statusStrip);
        Controls.Add(btnPanel);
        Controls.Add(topPanel);

        FormClosing += MainForm_FormClosing;
        Resize += MainForm_Resize;
    }

    // -------------------------------------------------------------------------
    // Tray
    // -------------------------------------------------------------------------

    private void InitializeTray()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Öffnen", null, (_, _) => ShowWindow());
        menu.Items.Add("Jetzt organisieren", null, (_, _) => RunOrganizer());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => ExitApp());

        _trayIcon = new NotifyIcon
        {
            Text = "Screenshot Organizer",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowWindow();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
            Hide();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closingToTray && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(2000, "Screenshot Organizer",
                "Fenster geschlossen. Task Scheduler läuft weiter (falls aktiv).", ToolTipIcon.Info);
        }
    }

    private void ExitApp()
    {
        _closingToTray = false;
        _trayIcon.Visible = false;
        Application.Exit();
    }

    // -------------------------------------------------------------------------
    // Core logic
    // -------------------------------------------------------------------------

    private void RunOrganizer()
    {
        _organizeBtn.Enabled = false;
        _statusLabel.Text = "Organisiere...";

        try
        {
            var result = new OrganizerService(_settings.ScreenshotFolder).Organize();

            if (result.TotalMoved == 0 && result.Errors.Count == 0)
            {
                AppendLog("Keine Dateien zum Verschieben gefunden.", Color.FromArgb(180, 180, 100));
            }
            else
            {
                foreach (var m in result.MovedFiles)
                    AppendLog("  ✓ " + m, Color.FromArgb(100, 220, 100));
                foreach (var err in result.Errors)
                    AppendLog("  ✗ " + err, Color.FromArgb(220, 80, 80));
                AppendLog($"Fertig: {result.TotalMoved} Datei(en) verschoben, {result.Errors.Count} Fehler.",
                    Color.FromArgb(220, 220, 220));
            }

            _settings.LastRun = DateTime.Now;
            _settings.Save();
            _statusLabel.Text = $"Zuletzt ausgeführt: {_settings.LastRun:dd.MM.yyyy HH:mm}";
        }
        catch (Exception ex)
        {
            AppendLog($"Schwerwiegender Fehler: {ex.Message}", Color.FromArgb(220, 60, 60));
            _statusLabel.Text = "Fehler aufgetreten.";
        }
        finally
        {
            _organizeBtn.Enabled = true;
        }
    }

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    private void OrganizeBtn_Click(object? sender, EventArgs e) => RunOrganizer();

    private void SettingsBtn_Click(object? sender, EventArgs e)
    {
        using var dlg = new SettingsForm(_settings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _settings = AppSettings.Load();
            RefreshStatus();
            AppendLog($"Einstellungen gespeichert. Task Scheduler: {(_settings.RunEvery2Hours ? "AN" : "AUS")}",
                Color.FromArgb(180, 180, 255));
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void RefreshStatus()
    {
        _folderValueLabel.Text = _settings.ScreenshotFolder;

        var taskRegistered = TaskSchedulerHelper.IsRegistered();
        if (_settings.RunEvery2Hours && taskRegistered)
        {
            _taskStatusLabel.Text = "Task Scheduler: aktiv — alle 2 Stunden";
            _taskStatusLabel.ForeColor = Color.FromArgb(40, 160, 40);
        }
        else if (_settings.RunEvery2Hours && !taskRegistered)
        {
            _taskStatusLabel.Text = "Task Scheduler: Einstellung aktiv, aber kein Task gefunden — bitte Einstellungen erneut öffnen";
            _taskStatusLabel.ForeColor = Color.OrangeRed;
        }
        else
        {
            _taskStatusLabel.Text = "Task Scheduler: inaktiv";
            _taskStatusLabel.ForeColor = Color.FromArgb(120, 120, 120);
        }

        _statusLabel.Text = _settings.LastRun.HasValue
            ? $"Zuletzt ausgeführt: {_settings.LastRun:dd.MM.yyyy HH:mm}"
            : "Noch nie ausgeführt";
    }

    // Load past auto-run entries from the log file written by --run mode
    private void LoadFileLog()
    {
        var lines = AppFileLogger.ReadRecentLines();
        if (lines.Length == 0)
        {
            AppendLog("Screenshot Organizer gestartet.", Color.FromArgb(100, 200, 100));
            return;
        }

        AppendLog("--- Bisherige Auto-Run-Einträge (aus Logdatei) ---", Color.FromArgb(100, 100, 160));
        foreach (var line in lines)
        {
            var color = line.StartsWith("  +") ? Color.FromArgb(100, 220, 100)
                      : line.StartsWith("  !") ? Color.FromArgb(220, 80, 80)
                      : line.StartsWith("[")   ? Color.FromArgb(180, 180, 255)
                      : Color.FromArgb(160, 160, 160);
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.SelectionColor = color;
            _logBox.AppendText(line + "\n");
        }
        AppendLog("--- Ende Logdatei ---", Color.FromArgb(100, 100, 160));
        AppendLog("Screenshot Organizer gestartet.", Color.FromArgb(100, 200, 100));
    }

    private void AppendLog(string message, Color color)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.SelectionLength = 0;
        _logBox.SelectionColor = Color.FromArgb(120, 120, 120);
        _logBox.AppendText($"[{ts}] ");
        _logBox.SelectionColor = color;
        _logBox.AppendText(message + "\n");
        _logBox.ScrollToCaret();
    }
}
