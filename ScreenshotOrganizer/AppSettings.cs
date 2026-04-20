using System.Text.Json;

namespace ScreenshotOrganizer;

public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenshotOrganizer", "settings.json");

    public string ScreenshotFolder { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");

    public bool RunEvery2Hours { get; set; } = false;
    public DateTime? LastRun { get; set; } = null;

    public static AppSettings Load()
    {
        AppSettings settings;
        try
        {
            settings = File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch
        {
            settings = new AppSettings();
        }

        // Keep RunEvery2Hours in sync with the actual Task Scheduler state.
        // Handles cases where the task was manually deleted or never registered.
        var taskExists = TaskSchedulerHelper.IsRegistered();
        if (settings.RunEvery2Hours && !taskExists)
        {
            // Setting says active but task is gone → correct the flag
            settings.RunEvery2Hours = false;
            settings.Save();
        }
        else if (!settings.RunEvery2Hours && taskExists)
        {
            // Orphaned task (e.g. setting was reset) → remove it
            TaskSchedulerHelper.Unregister();
        }

        return settings;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
