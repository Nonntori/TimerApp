using System.Windows.Threading;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

/// <summary>
/// Service for managing the countdown timer
/// </summary>
public class TimerService : IDisposable
{
    private DispatcherTimer? _timer;
    private DateTimeOffset? _endTime;
    private ShutdownAction _action;
    private bool _isRunning;
    private bool _warningShown;
    private readonly ShutdownService _shutdownService;
    private readonly NotificationService _notificationService;
    private readonly SettingsService _settingsService;

    public event EventHandler<TimeSpan>? Tick;
    public event EventHandler? TimerCompleted;
    public event EventHandler? WarningTriggered;

    public bool IsRunning => _isRunning;
    public TimeSpan RemainingTime => _endTime.HasValue 
        ? _endTime.Value - DateTimeOffset.Now 
        : TimeSpan.Zero;
    public double Progress => _endTime.HasValue && _startTime.HasValue
        ? Math.Max(0, Math.Min(1, (double)RemainingTime.TotalSeconds / _totalSeconds))
        : 1.0;

    private DateTimeOffset? _startTime;
    private double _totalSeconds;

    public TimerService(
        ShutdownService shutdownService,
        NotificationService notificationService,
        SettingsService settingsService)
    {
        _shutdownService = shutdownService;
        _notificationService = notificationService;
        _settingsService = settingsService;
    }

    public async Task StartAsync(TimeSpan duration, ShutdownAction action)
    {
        await Task.Run(() => Start(duration, action));
    }

    public void Start(TimeSpan duration, ShutdownAction action)
    {
        Stop();

        _action = action;
        _endTime = DateTimeOffset.Now + duration;
        _startTime = DateTimeOffset.Now;
        _totalSeconds = duration.TotalSeconds;
        _isRunning = true;
        _warningShown = false;

        // Schedule Windows shutdown for actions that support it
        if (action == ShutdownAction.Shutdown || action == ShutdownAction.Restart)
        {
            _shutdownService.ScheduleShutdown(action, duration);
        }

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        // Show notification
        _notificationService.ShowNotification(
            "Таймер запущен",
            GetActionName(action) + " через " + FormatDuration(duration));
    }

    public void Stop()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        if (_isRunning)
        {
            _shutdownService.CancelShutdown();
            _isRunning = false;
            _endTime = null;
            _warningShown = false;

            _notificationService.ShowNotification(
                "Таймер отменён",
                "Запланированное действие было отменено");
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_endTime.HasValue) return;

        var remaining = _endTime.Value - DateTimeOffset.Now;

        if (remaining <= TimeSpan.Zero)
        {
            CompleteTimer();
            return;
        }

        // Check for warning
        CheckWarning(remaining);

        Tick?.Invoke(this, remaining);
    }

    private void CheckWarning(TimeSpan remaining)
    {
        if (_warningShown) return;

        int warningMinutes = _settingsService.Settings.WarningMinutes;
        var warningThreshold = TimeSpan.FromMinutes(warningMinutes);

        if (remaining <= warningThreshold && remaining > TimeSpan.Zero)
        {
            _warningShown = true;
            WarningTriggered?.Invoke(this, EventArgs.Empty);

            if (_settingsService.Settings.ShowNotifications)
            {
                _notificationService.ShowNotification(
                    "Предупреждение",
                    $"Компьютер будет {_GetActionVerb(_action)} через {warningMinutes} мин.");
            }

            if (_settingsService.Settings.SoundEnabled)
            {
                PlayWarningSound();
            }
        }
    }

    private string _GetActionVerb(ShutdownAction action)
    {
        return action switch
        {
            ShutdownAction.Shutdown => "выключен",
            ShutdownAction.Restart => "перезагружен",
            ShutdownAction.LogOff => "выполнен выход",
            ShutdownAction.Sleep => "переведён в спящий режим",
            _ => "выполнено действие"
        };
    }

    private void CompleteTimer()
    {
        Stop();
        
        // Execute the action
        if (_action == ShutdownAction.Shutdown || _action == ShutdownAction.Restart)
        {
            // Windows shutdown is already scheduled, just let it happen
            _notificationService.ShowNotification(
                "Выполнение действия",
                "Запланированная операция выполняется...");
        }
        else
        {
            // For logoff and sleep, execute immediately
            _shutdownService.ExecuteImmediateAction(_action);
        }

        TimerCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void PlayWarningSound()
    {
        try
        {
            System.Media.SystemSounds.Exclamation.Play();
        }
        catch { }
    }

    public static string FormatDuration(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours} ч {(int)time.Minutes} мин";
        }
        else if (time.TotalMinutes >= 1)
        {
            return $"{(int)time.TotalMinutes} мин {(int)time.Seconds} сек";
        }
        else
        {
            return $"{(int)time.Seconds} сек";
        }
    }

    private string GetActionName(ShutdownAction action)
    {
        return action switch
        {
            ShutdownAction.Shutdown => "Выключение компьютера",
            ShutdownAction.Restart => "Перезагрузка",
            ShutdownAction.LogOff => "Выход из системы",
            ShutdownAction.Sleep => "Спящий режим",
            _ => "Действие"
        };
    }

    public void Dispose()
    {
        Stop();
    }

    // For restoring state after app restart
    public void RestoreState(DateTimeOffset endTime, ShutdownAction action)
    {
        var remaining = endTime - DateTimeOffset.Now;
        if (remaining > TimeSpan.Zero)
        {
            _endTime = endTime;
            _action = action;
            _isRunning = true;
            
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
    }
}
