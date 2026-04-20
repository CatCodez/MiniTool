namespace ScreenshotOrganizer;

public class MainForm : Form
{
    private AppSettings _settings;
    private System.Windows.Forms.Timer _autoTimer = null!;
    private NotifyIcon _trayIcon = null!;

    private Label _folderValueLabel = null!;
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
        InitializeTimer();
        UpdateFolderLabel();

        // Run immediately on startup if overdue
        if (_settings.RunEvery2Hours && IsOverdue())
            RunOrganizer();
    }

    // -------------------------------------------------------------------------
    // UI Setup
    // -------------------------------------------------------------------------

    private void InitializeComponents()
    {
        Text = "Screenshot Organizer";
        Size = new Size(660, 520);
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.CenterScreen;

        // --- Top panel: folder info ---
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(12, 8, 12, 4) };
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
        topPanel.Controls.Add(folderCaption);
        topPanel.Controls.Add(_folderValueLabel);

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

        AppendLog("Screenshot Organizer gestartet.", Color.FromArgb(100, 200, 100));
        AppendLog($"Ordner: {_settings.ScreenshotFolder}", Color.FromArgb(180, 180, 180));
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
                "Läuft im Hintergrund weiter.", ToolTipIcon.Info);
        }
    }

    private void ExitApp()
    {
        _closingToTray = false;
        _trayIcon.Visible = false;
        Application.Exit();
    }

    // -------------------------------------------------------------------------
    // Timer
    // -------------------------------------------------------------------------

    private void InitializeTimer()
    {
        _autoTimer = new System.Windows.Forms.Timer { Interval = 2 * 60 * 60 * 1000 }; // 2 hours
        _autoTimer.Tick += AutoTimer_Tick;
        _autoTimer.Enabled = _settings.RunEvery2Hours;
    }

    private void AutoTimer_Tick(object? sender, EventArgs e)
    {
        AppendLog("Automatische Ausführung (Timer)...", Color.FromArgb(100, 180, 255));
        RunOrganizer();
    }

    private bool IsOverdue()
    {
        if (_settings.LastRun is null) return true;
        return (DateTime.Now - _settings.LastRun.Value).TotalHours >= 2;
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
            var service = new OrganizerService(_settings.ScreenshotFolder);
            var result = service.Organize();

            if (result.TotalMoved == 0 && result.Errors.Count == 0)
            {
                AppendLog("Keine Dateien zum Verschieben gefunden.", Color.FromArgb(180, 180, 100));
            }
            else
            {
                foreach (var moved in result.MovedFiles)
                    AppendLog("  ✓ " + moved, Color.FromArgb(100, 220, 100));

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
            _autoTimer.Enabled = _settings.RunEvery2Hours;
            UpdateFolderLabel();
            AppendLog($"Einstellungen gespeichert. Auto-Run: {(_settings.RunEvery2Hours ? "AN" : "AUS")}",
                Color.FromArgb(180, 180, 255));
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void UpdateFolderLabel()
    {
        _folderValueLabel.Text = _settings.ScreenshotFolder;
        if (_settings.LastRun.HasValue)
            _statusLabel.Text = $"Zuletzt ausgeführt: {_settings.LastRun:dd.MM.yyyy HH:mm}";
    }

    private void AppendLog(string message, Color color)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.SelectionLength = 0;

        _logBox.SelectionColor = Color.FromArgb(120, 120, 120);
        _logBox.AppendText($"[{timestamp}] ");

        _logBox.SelectionColor = color;
        _logBox.AppendText(message + "\n");

        _logBox.ScrollToCaret();
    }
}
