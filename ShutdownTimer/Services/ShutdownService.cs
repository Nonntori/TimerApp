using System.Diagnostics;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

/// <summary>
/// Service for executing Windows shutdown operations
/// </summary>
public class ShutdownService
{
    private bool _shutdownScheduled;
    private readonly object _lock = new();

    public bool IsShutdownScheduled => _shutdownScheduled;

    /// <summary>
    /// Schedule a shutdown action
    /// </summary>
    public async Task<bool> ScheduleShutdown(ShutdownAction action, TimeSpan delay)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    int delaySeconds = (int)delay.TotalSeconds;

                    switch (action)
                    {
                        case ShutdownAction.Shutdown:
                            // Cancel any existing shutdown first
                            ExecuteShutdownCommand("/a", true);
                            // Schedule new shutdown
                            return ExecuteShutdownCommand($"/s /t {delaySeconds}");

                        case ShutdownAction.Restart:
                            ExecuteShutdownCommand("/a", true);
                            return ExecuteShutdownCommand($"/r /t {delaySeconds}");

                        case ShutdownAction.LogOff:
                            // Logoff doesn't support delay, we handle it differently
                            // For now, just schedule a shutdown that will trigger logoff script
                            // Actually, we'll handle this in the timer when it completes
                            return true;

                        case ShutdownAction.Sleep:
                            // Sleep also handled at completion time
                            return true;

                        default:
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to schedule shutdown: {ex.Message}");
                    return false;
                }
            }
        });
    }

    /// <summary>
    /// Cancel scheduled shutdown
    /// </summary>
    public bool CancelShutdown()
    {
        lock (_lock)
        {
            try
            {
                _shutdownScheduled = false;
                return ExecuteShutdownCommand("/a");
            }
            catch (Exception ex)
            {
                LogError($"Failed to cancel shutdown: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Execute immediate action (for actions without delay support)
    /// </summary>
    public void ExecuteImmediateAction(ShutdownAction action)
    {
        try
        {
            switch (action)
            {
                case ShutdownAction.Shutdown:
                    ExecuteShutdownCommand("/s /t 0");
                    break;

                case ShutdownAction.Restart:
                    ExecuteShutdownCommand("/r /t 0");
                    break;

                case ShutdownAction.LogOff:
                    // Use Windows API for logoff
                    var psi = new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/l",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                    break;

                case ShutdownAction.Sleep:
                    // Use SetSuspendState via P/Invoke
                    SetSystemSleep();
                    break;
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to execute immediate action: {ex.Message}");
        }
    }

    private bool ExecuteShutdownCommand(string arguments, bool ignoreErrors = false)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(5000);
                
                if (arguments.Contains("/a"))
                {
                    // Abort command returns non-zero if no shutdown was scheduled
                    return true; // Consider it success even if nothing to abort
                }
                
                _shutdownScheduled = process.ExitCode == 0;
                return process.ExitCode == 0;
            }
            return false;
        }
        catch (Exception ex) when (ignoreErrors)
        {
            return false;
        }
        catch (Exception ex)
        {
            LogError($"Shutdown command failed: {ex.Message}");
            throw;
        }
    }

    [System.Runtime.InteropServices.DllImport("powrprof.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    private void SetSystemSleep()
    {
        try
        {
            SetSuspendState(false, true, false);
        }
        catch (Exception ex)
        {
            LogError($"Failed to enter sleep mode: {ex.Message}");
            
            // Alternative: use powercfg
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = "powrprof.dll,SetSuspendState 0,1,0",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch { }
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
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [SHUTDOWN ERROR] {message}{Environment.NewLine}");
        }
        catch { }
    }
}
