namespace ScreenshotOrganizer;

public static class AppFileLogger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenshotOrganizer", "run.log");

    public static void Write(OrganizerResult result)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

            var lines = new List<string>
            {
                $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] Auto-Run: {result.TotalMoved} Datei(en) verschoben"
            };
            foreach (var m in result.MovedFiles) lines.Add("  + " + m);
            foreach (var e in result.Errors)     lines.Add("  ! " + e);
            lines.Add(string.Empty);

            File.AppendAllLines(LogPath, lines);
            TrimLog();
        }
        catch { }
    }

    public static string[] ReadRecentLines(int count = 300)
    {
        try
        {
            if (!File.Exists(LogPath)) return [];
            var all = File.ReadAllLines(LogPath);
            return all.Length <= count ? all : all[^count..];
        }
        catch { return []; }
    }

    private static void TrimLog()
    {
        try
        {
            var lines = File.ReadAllLines(LogPath);
            if (lines.Length > 1000)
                File.WriteAllLines(LogPath, lines[^1000..]);
        }
        catch { }
    }
}
