namespace ScreenshotOrganizer;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private TextBox _folderBox = null!;
    private CheckBox _autoRunCheck = null!;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        InitializeComponents();
        LoadValues();
    }

    private void InitializeComponents()
    {
        Text = "Einstellungen";
        Size = new Size(520, 220);
        MinimumSize = new Size(440, 220);
        MaximumSize = new Size(800, 220);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // --- Folder row ---
        var folderLabel = new Label
        {
            Text = "Screenshot-Ordner:",
            AutoSize = true,
            Location = new Point(16, 20)
        };

        _folderBox = new TextBox
        {
            Location = new Point(16, 42),
            Size = new Size(380, 24),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };

        var browseBtn = new Button
        {
            Text = "...",
            Location = new Point(404, 41),
            Size = new Size(80, 26)
        };
        browseBtn.Click += BrowseBtn_Click;

        // --- Auto-run checkbox ---
        _autoRunCheck = new CheckBox
        {
            Text = "Alle 2 Stunden automatisch ausführen",
            AutoSize = true,
            Location = new Point(16, 84)
        };

        // --- OK / Cancel ---
        var okBtn = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(88, 30),
            Location = new Point(316, 136)
        };
        okBtn.Click += OkBtn_Click;

        var cancelBtn = new Button
        {
            Text = "Abbrechen",
            DialogResult = DialogResult.Cancel,
            Size = new Size(88, 30),
            Location = new Point(412, 136)
        };

        AcceptButton = okBtn;
        CancelButton = cancelBtn;

        Controls.AddRange(new Control[]
        {
            folderLabel, _folderBox, browseBtn, _autoRunCheck, okBtn, cancelBtn
        });
    }

    private void LoadValues()
    {
        _folderBox.Text = _settings.ScreenshotFolder;
        _autoRunCheck.Checked = _settings.RunEvery2Hours;
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
        _settings.RunEvery2Hours = _autoRunCheck.Checked;
        _settings.Save();
    }
}
