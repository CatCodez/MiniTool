namespace ScreenshotOrganizer;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private TextBox _folderBox = null!;
    private CheckBox _autoRunCheck = null!;
    private Label _taskStatusLabel = null!;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        InitializeComponents();
        LoadValues();
        RefreshTaskStatus();
    }

    private void InitializeComponents()
    {
        Text = "Einstellungen";
        Size = new Size(540, 260);
        MinimumSize = new Size(460, 260);
        MaximumSize = new Size(800, 260);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // --- Folder row ---
        var folderLabel = new Label { Text = "Screenshot-Ordner:", AutoSize = true, Location = new Point(16, 20) };

        _folderBox = new TextBox
        {
            Location = new Point(16, 42),
            Size = new Size(390, 24),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };

        var browseBtn = new Button { Text = "...", Location = new Point(414, 41), Size = new Size(80, 26) };
        browseBtn.Click += BrowseBtn_Click;

        // --- Auto-run checkbox ---
        _autoRunCheck = new CheckBox
        {
            Text = "Alle 2 Stunden via Task Scheduler ausführen (App muss nicht offen sein)",
            AutoSize = true,
            Location = new Point(16, 84)
        };
        _autoRunCheck.CheckedChanged += (_, _) => _taskStatusLabel.Visible = false;

        // --- Task status hint ---
        _taskStatusLabel = new Label
        {
            AutoSize = true,
            Location = new Point(34, 108),
            ForeColor = Color.Gray,
            Font = new Font(Font.FontFamily, 8f),
            Visible = false
        };

        // --- OK / Cancel ---
        var okBtn = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(88, 30),
            Location = new Point(330, 192)
        };
        okBtn.Click += OkBtn_Click;

        var cancelBtn = new Button
        {
            Text = "Abbrechen",
            DialogResult = DialogResult.Cancel,
            Size = new Size(88, 30),
            Location = new Point(426, 192)
        };

        AcceptButton = okBtn;
        CancelButton = cancelBtn;

        Controls.AddRange(new Control[]
        {
            folderLabel, _folderBox, browseBtn,
            _autoRunCheck, _taskStatusLabel,
            okBtn, cancelBtn
        });
    }

    private void LoadValues()
    {
        _folderBox.Text = _settings.ScreenshotFolder;
        _autoRunCheck.Checked = _settings.RunEvery2Hours;
    }

    private void RefreshTaskStatus()
    {
        var registered = TaskSchedulerHelper.IsRegistered();
        _taskStatusLabel.Text = registered
            ? "Task Scheduler-Eintrag vorhanden."
            : "Kein Task Scheduler-Eintrag vorhanden.";
        _taskStatusLabel.ForeColor = registered ? Color.Green : Color.Gray;
        _taskStatusLabel.Visible = true;
    }

    private void BrowseBtn_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Screenshot-Ordner auswählen",
            SelectedPath = _folderBox.Text
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _folderBox.Text = dlg.SelectedPath;
    }

    private void OkBtn_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_folderBox.Text))
        {
            MessageBox.Show("Bitte einen gültigen Ordnerpfad angeben.", "Validierung",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        _settings.ScreenshotFolder = _folderBox.Text.Trim();
        var wasEnabled = _settings.RunEvery2Hours;
        _settings.RunEvery2Hours = _autoRunCheck.Checked;
        _settings.Save();

        // Register or remove Task Scheduler entry if the setting changed
        if (_settings.RunEvery2Hours && !wasEnabled)
        {
            var (ok, msg) = TaskSchedulerHelper.Register();
            if (!ok)
            {
                MessageBox.Show(
                    $"Task Scheduler-Eintrag konnte nicht erstellt werden:\n{msg}\n\n" +
                    "Tipp: Starte die App einmalig als Administrator.",
                    "Task Scheduler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _settings.RunEvery2Hours = false;
                _settings.Save();
                DialogResult = DialogResult.None;
                return;
            }
        }
        else if (!_settings.RunEvery2Hours && wasEnabled)
        {
            TaskSchedulerHelper.Unregister();
        }
    }
}
