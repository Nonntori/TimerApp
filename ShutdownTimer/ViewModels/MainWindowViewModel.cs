using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

/// <summary>
/// Main window ViewModel
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly TimerService _timerService;
    private readonly ShutdownService _shutdownService;
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;
    private readonly TrayIconService _trayIconService;

    [ObservableProperty]
    private TimeSpan _remainingTime = TimeSpan.FromMinutes(30);

    [ObservableProperty]
    private double _progress = 1.0;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private ShutdownAction _selectedAction = ShutdownAction.Shutdown;

    [ObservableProperty]
    private TimePreset _selectedPreset = TimePreset.Minutes30;

    [ObservableProperty]
    private bool _warningEnabled = true;

    [ObservableProperty]
    private int _warningMinutes = 5;

    [ObservableProperty]
    private string _statusText = "Компьютер выключится по истечении времени";

    [ObservableProperty]
    private bool _isWarningVisible;

    [ObservableProperty]
    private int _customHours;

    [ObservableProperty]
    private int _customMinutes;

    [ObservableProperty]
    private int _customSeconds;

    [ObservableProperty]
    private bool _isCustomTimeDialogOpen;

    public MainWindowViewModel(
        TimerService timerService,
        ShutdownService shutdownService,
        SettingsService settingsService,
        NotificationService notificationService,
        TrayIconService trayIconService)
    {
        _timerService = timerService;
        _shutdownService = shutdownService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _trayIconService = trayIconService;

        // Load settings
        _warningMinutes = settingsService.Settings.WarningMinutes;
        _selectedAction = settingsService.Settings.DefaultAction;
        _warningEnabled = settingsService.Settings.ShowNotifications;

        // Subscribe to timer events
        _timerService.Tick += OnTimerTick;
        _timerService.TimerCompleted += OnTimerCompleted;
        _timerService.WarningTriggered += OnWarningTriggered;
    }

    private void OnTimerTick(object? sender, TimeSpan remaining)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            RemainingTime = remaining;
            Progress = _timerService.Progress;
            
            // Update tray icon
            _trayIconService.UpdateTimerStatus(true, remaining);
        });
    }

    private void OnTimerCompleted(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsRunning = false;
            StatusText = "Действие выполняется...";
            Progress = 0;
        });
    }

    private void OnWarningTriggered(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsWarningVisible = true;
        });
    }

    [RelayCommand]
    private void SelectPreset(TimePreset preset)
    {
        if (IsRunning) return;

        SelectedPreset = preset;
        
        switch (preset)
        {
            case TimePreset.Minutes15:
                RemainingTime = TimeSpan.FromMinutes(15);
                break;
            case TimePreset.Minutes30:
                RemainingTime = TimeSpan.FromMinutes(30);
                break;
            case TimePreset.Hour1:
                RemainingTime = TimeSpan.FromHours(1);
                break;
            case TimePreset.Hour2:
                RemainingTime = TimeSpan.FromHours(2);
                break;
            case TimePreset.Custom:
                OpenCustomTimeDialog();
                return;
        }

        Progress = 1.0;
    }

    [RelayCommand]
    private void OpenCustomTimeDialog()
    {
        CustomHours = (int)RemainingTime.TotalHours;
        CustomMinutes = RemainingTime.Minutes;
        CustomSeconds = RemainingTime.Seconds;
        IsCustomTimeDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCustomTimeDialog()
    {
        IsCustomTimeDialogOpen = false;
    }

    [RelayCommand]
    private void ApplyCustomTime()
    {
        if (CustomHours < 0 || CustomMinutes < 0 || CustomMinutes >= 60 || 
            CustomSeconds < 0 || CustomSeconds >= 60)
        {
            _notificationService.ShowNotification("Ошибка", "Некорректное время");
            return;
        }

        var totalSeconds = CustomHours * 3600 + CustomMinutes * 60 + CustomSeconds;
        if (totalSeconds <= 0)
        {
            _notificationService.ShowNotification("Ошибка", "Время должно быть больше 0");
            return;
        }

        RemainingTime = TimeSpan.FromSeconds(totalSeconds);
        SelectedPreset = TimePreset.Custom;
        IsCustomTimeDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(CanStartTimer))]
    private async Task StartTimer()
    {
        if (RemainingTime <= TimeSpan.Zero)
        {
            _notificationService.ShowNotification("Ошибка", "Выберите время больше 0");
            return;
        }

        // Show confirmation
        var actionName = GetActionName(SelectedAction);
        var timeStr = TimerService.FormatDuration(RemainingTime);
        
        var result = MessageBox.Show(
            $"Вы уверены, что хотите запланировать {actionName.ToLower()} через {timeStr}?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        IsRunning = true;
        StatusText = $"{GetActionVerb(SelectedAction)} через";
        
        await _timerService.StartAsync(RemainingTime, SelectedAction);
    }

    [RelayCommand]
    private void CancelTimer()
    {
        _timerService.Stop();
        IsRunning = false;
        StatusText = "Компьютер выключится по истечении времени";
        Progress = 1.0;
        SelectedPreset = TimePreset.None;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // Open settings window
    }

    private bool CanStartTimer()
    {
        return !IsRunning && RemainingTime > TimeSpan.Zero;
    }

    private string GetActionName(ShutdownAction action)
    {
        return action switch
        {
            ShutdownAction.Shutdown => "выключение компьютера",
            ShutdownAction.Restart => "перезагрузку",
            ShutdownAction.LogOff => "выход из системы",
            ShutdownAction.Sleep => "переход в спящий режим",
            _ => "действие"
        };
    }

    private string GetActionVerb(ShutdownAction action)
    {
        return action switch
        {
            ShutdownAction.Shutdown => "Выключение",
            ShutdownAction.Restart => "Перезагрузка",
            ShutdownAction.LogOff => "Выход",
            ShutdownAction.Sleep => "Спящий режим",
            _ => "Действие"
        };
    }

    public void SaveState()
    {
        _settingsService.UpdateSettings(s =>
        {
            s.WarningMinutes = WarningMinutes;
            s.DefaultAction = SelectedAction;
            s.ShowNotifications = WarningEnabled;
        });
    }
}
