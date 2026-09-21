using System.Windows;
using FormsApp = System.Windows.Forms.Application;

namespace ShutdownTimer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Log application start
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "ShutdownTimer", "logs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
        
        var logPath = Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
        try
        {
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] Application started{Environment.NewLine}");
        }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Log application exit
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "ShutdownTimer", "logs");
        if (Directory.Exists(logDir))
        {
            var logPath = Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
            try
            {
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] Application exited{Environment.NewLine}");
            }
            catch { }
        }
        
        base.OnExit(e);
    }
}
