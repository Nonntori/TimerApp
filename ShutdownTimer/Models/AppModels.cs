namespace ShutdownTimer.Models;

/// <summary>
/// Represents available shutdown actions
/// </summary>
public enum ShutdownAction
{
    Shutdown,      // Выключить компьютер
    Restart,       // Перезагрузить
    LogOff,        // Выйти из системы
    Sleep          // Спящий режим
}

/// <summary>
/// Available time presets
/// </summary>
public enum TimePreset
{
    None,
    Minutes15,
    Minutes30,
    Hour1,
    Hour2,
    Custom
}

/// <summary>
/// Application settings model
/// </summary>
public class AppSettings
{
    public bool AutoStart { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public int WarningMinutes { get; set; } = 5;
    public bool SoundEnabled { get; set; } = true;
    public ShutdownAction DefaultAction { get; set; } = ShutdownAction.Shutdown;
    public string Theme { get; set; } = "Dark"; // Dark, Light, System
    public string Language { get; set; } = "ru";
}

/// <summary>
/// Timer state for persistence
/// </summary>
public class TimerState
{
    public bool IsRunning { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public ShutdownAction Action { get; set; }
    public TimeSpan RemainingTime => EndTime.HasValue 
        ? EndTime.Value - DateTimeOffset.Now 
        : TimeSpan.Zero;
}
