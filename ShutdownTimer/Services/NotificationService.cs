using Microsoft.Win32;
using System.Drawing;
using System.Windows.Forms;

namespace ShutdownTimer.Services;

/// <summary>
/// Service for Windows toast notifications
/// </summary>
public class NotificationService
{
    private bool _isInitialized;

    public void ShowNotification(string title, string message)
    {
        try
        {
            // Use Windows Forms NotifyIcon for compatibility
            ShowTrayNotification(title, message);
        }
        catch (Exception ex)
        {
            LogError($"Failed to show notification: {ex.Message}");
        }
    }

    private void ShowTrayNotification(string title, string message)
    {
        // For WPF apps, we'll use a simple approach
        // In production, you'd want to use Windows.UI.Notifications
        System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] {title}: {message}");
        
        // Play system notification sound
        try
        {
            System.Media.SystemSounds.Notification.Play();
        }
        catch { }
    }

    private void LogError(string message)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "ShutdownTimer", "logs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
        
        var logPath = Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
        try
        {
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NOTIFY ERROR] {message}{Environment.NewLine}");
        }
        catch { }
    }
}

/// <summary>
/// Service for managing system tray icon
/// </summary>
public class TrayIconService : IDisposable
{
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _contextMenu;
    private readonly TimerService _timerService;
    private Action? _onOpen;
    private Action? _onExit;
    private Action? _onCancelTimer;

    public TrayIconService(TimerService timerService)
    {
        _timerService = timerService;
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _contextMenu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem("Открыть");
        openItem.Click += (s, e) => _onOpen?.Invoke();
        _contextMenu.Items.Add(openItem);

        var cancelItem = new ToolStripMenuItem("Отменить таймер");
        cancelItem.Click += (s, e) => _onCancelTimer?.Invoke();
        _contextMenu.Items.Add(cancelItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Выход");
        exitItem.Click += (s, e) => _onExit?.Invoke();
        _contextMenu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = CreateAppIcon(),
            Text = "Таймер выключения",
            ContextMenuStrip = _contextMenu,
            Visible = false
        };

        _trayIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _onOpen?.Invoke();
            }
        };
    }

    private Icon CreateAppIcon()
    {
        // Create a simple power button icon programmatically
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Background circle
            using (var brush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            {
                g.FillEllipse(brush, 2, 2, 28, 28);
            }
            
            // Power symbol - vertical line
            using (var pen = new Pen(Color.FromArgb(100, 180, 255), 3))
            {
                g.DrawLine(pen, 16, 6, 16, 16);
                
                // Power symbol - arc
                using (var arcPen = new Pen(Color.FromArgb(100, 180, 255), 3))
                {
                    g.DrawArc(arcPen, 8, 8, 16, 16, 135, 270);
                }
            }
        }
        
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void SetCallbacks(Action onOpen, Action onCancelTimer, Action onExit)
    {
        _onOpen = onOpen;
        _onCancelTimer = onCancelTimer;
        _onExit = onExit;
    }

    public void Show(bool show = true)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = show;
        }
    }

    public void UpdateTooltip(string text)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Text = text;
        }
    }

    public void UpdateTimerStatus(bool isRunning, TimeSpan remaining)
    {
        if (isRunning)
        {
            UpdateTooltip($"Выключение через {remaining:hh\\:mm\\:ss}");
        }
        else
        {
            UpdateTooltip("Таймер выключения");
        }
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _contextMenu?.Dispose();
    }
}

/// <summary>
/// Service for managing Windows startup
/// </summary>
public class StartupService
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ShutdownTimer";

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (enable)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? 
                              Application.ResourceAssembly.Location;
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key?.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to set auto-start: {ex.Message}");
        }
    }

    private void LogError(string message)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "ShutdownTimer", "logs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
        
        var logPath = Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
        try
        {
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [STARTUP ERROR] {message}{Environment.NewLine}");
        }
        catch { }
    }
}
