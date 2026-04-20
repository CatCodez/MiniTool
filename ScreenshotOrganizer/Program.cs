namespace ScreenshotOrganizer;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("--run"))
        {
            RunSilent();
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    // Called by Task Scheduler: no UI, just organize and log.
    private static void RunSilent()
    {
        var settings = AppSettings.Load();
        var result = new OrganizerService(settings.ScreenshotFolder).Organize();
        settings.LastRun = DateTime.Now;
        settings.Save();
        AppFileLogger.Write(result);
    }
}
