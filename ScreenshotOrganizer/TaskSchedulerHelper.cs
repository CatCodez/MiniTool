using System.Diagnostics;

namespace ScreenshotOrganizer;

public static class TaskSchedulerHelper
{
    private const string TaskName = "ScreenshotOrganizer_AutoRun";

    public static (bool Success, string Message) Register()
    {
        var exePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(exePath))
            return (false, "Pfad zur ausführbaren Datei konnte nicht ermittelt werden.");

        // HOURLY /mo 2 = alle 2 Stunden; /f überschreibt bestehenden Task
        var args = $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --run\" /sc HOURLY /mo 2 /f";
        return RunSchtasks(args);
    }

    public static (bool Success, string Message) Unregister()
    {
        if (!IsRegistered()) return (true, "Task war nicht vorhanden.");
        return RunSchtasks($"/delete /tn \"{TaskName}\" /f");
    }

    public static bool IsRegistered()
    {
        var (ok, _) = RunSchtasks($"/query /tn \"{TaskName}\"");
        return ok;
    }

    private static (bool Success, string Message) RunSchtasks(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks.exe", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi)!;
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode == 0, (stdout + stderr).Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
