namespace ScreenshotOrganizer;

public class OrganizerResult
{
    public List<string> MovedFiles { get; } = [];
    public List<string> Errors { get; } = [];
    public int TotalMoved => MovedFiles.Count;
}

public class OrganizerService
{
    private readonly string _folder;

    public OrganizerService(string folder)
    {
        _folder = folder;
    }

    public OrganizerResult Organize()
    {
        var result = new OrganizerResult();
        var now = DateTime.Now;

        if (!Directory.Exists(_folder))
        {
            result.Errors.Add($"Ordner nicht gefunden: {_folder}");
            return result;
        }

        foreach (var filePath in Directory.GetFiles(_folder, "*.png", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var info = new FileInfo(filePath);
                var ageDays = (now - info.LastWriteTime).TotalDays;

                string? targetDir = null;

                if (ageDays > 90)
                {
                    var dayFolder = info.LastWriteTime.ToString("yyyy-MM-dd");
                    targetDir = Path.Combine(_folder, "Archiv", "3 Monate", dayFolder);
                }
                else if (ageDays > 30)
                {
                    var dayFolder = info.LastWriteTime.ToString("yyyy-MM-dd");
                    targetDir = Path.Combine(_folder, "Archiv", "1 Monat", dayFolder);
                }
                else if (ageDays > 7)
                {
                    targetDir = Path.Combine(_folder, "Archiv", "1 Woche");
                }

                if (targetDir is null)
                    continue;

                Directory.CreateDirectory(targetDir);
                var destPath = ResolveDestPath(targetDir, info.Name);
                File.Move(filePath, destPath);

                var relTarget = Path.GetRelativePath(_folder, destPath);
                result.MovedFiles.Add($"{info.Name}  →  {relTarget}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Fehler bei '{Path.GetFileName(filePath)}': {ex.Message}");
            }
        }

        return result;
    }

    private static string ResolveDestPath(string dir, string fileName)
    {
        var dest = Path.Combine(dir, fileName);
        if (!File.Exists(dest))
            return dest;

        var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var counter = 1;
        do
        {
            dest = Path.Combine(dir, $"{nameNoExt} ({counter++}){ext}");
        } while (File.Exists(dest));

        return dest;
    }
}
