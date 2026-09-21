using System.Windows;
using System.Windows.Controls;
using ShutdownTimer.Models;
using ShutdownTimer.Services;
using ShutdownTimer.ViewModels;

namespace ShutdownTimer.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly TimerService _timerService;
    private readonly SettingsService _settingsService;
    private readonly TrayIconService _trayIconService;

    public MainWindow(
        MainWindowViewModel viewModel,
        TimerService timerService,
        SettingsService settingsService,
        TrayIconService trayIconService)
    {
        InitializeComponent();
        
        _viewModel = viewModel;
        _timerService = timerService;
        _settingsService = settingsService;
        _trayIconService = trayIconService;
        
        DataContext = _viewModel;
        
        // Setup tray icon callbacks
        _trayIconService.SetCallbacks(
            onOpen: () => 
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                });
            },
            onCancelTimer: () =>
            {
                if (_viewModel.IsRunning)
                {
                    _viewModel.CancelTimerCommand.Execute(null);
                }
            },
            onExit: () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _viewModel.SaveState();
                    _trayIconService.Show(false);
                    Application.Current.Shutdown();
                });
            }
        );
        
        // Subscribe to timer updates
        _timerService.Tick += (s, remaining) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateButtonText();
                UpdateTrayIcon(remaining);
            });
        };
        
        UpdateButtonText();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_viewModel.IsRunning && _settingsService.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            
            var result = MessageBox.Show(
                "Таймер продолжит работать в фоновом режиме.\n\nСвернуть в трей или отменить таймер?",
                "Таймер активен",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Information);
            
            if (result == MessageBoxResult.Yes)
            {
                Hide();
                _trayIconService.Show(true);
            }
            else if (result == MessageBoxResult.No)
            {
                _viewModel.CancelTimerCommand.Execute(null);
                _trayIconService.Show(false);
            }
            // Cancel = do nothing
        }
        else
        {
            _viewModel.SaveState();
            _trayIconService.Show(false);
        }
        
        base.OnClosing(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        
        if (WindowState == WindowState.Minimized && 
            _settingsService.Settings.MinimizeToTray)
        {
            Hide();
            _trayIconService.Show(true);
        }
    }

    private void MinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void StartStopButtonClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsRunning)
        {
            _viewModel.CancelTimerCommand.Execute(null);
        }
        else
        {
            _ = _viewModel.StartTimerCommand.ExecuteAsync(null);
        }
    }

    private void Preset15MinClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.IsRunning)
        {
            _viewModel.RemainingTime = TimeSpan.FromMinutes(15);
            _viewModel.SelectedPreset = TimePreset.Minutes15;
            _viewModel.Progress = 1.0;
        }
    }

    private void Preset30MinClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.IsRunning)
        {
            _viewModel.RemainingTime = TimeSpan.FromMinutes(30);
            _viewModel.SelectedPreset = TimePreset.Minutes30;
            _viewModel.Progress = 1.0;
        }
    }

    private void Preset1HourClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.IsRunning)
        {
            _viewModel.RemainingTime = TimeSpan.FromHours(1);
            _viewModel.SelectedPreset = TimePreset.Hour1;
            _viewModel.Progress = 1.0;
        }
    }

    private void Preset2HourClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.IsRunning)
        {
            _viewModel.RemainingTime = TimeSpan.FromHours(2);
            _viewModel.SelectedPreset = TimePreset.Hour2;
            _viewModel.Progress = 1.0;
        }
    }

    private void UpdateButtonText()
    {
        if (StartStopButton != null)
        {
            StartStopButton.Content = _viewModel.IsRunning 
                ? "⏻ Остановить таймер" 
                : "⏻ Запустить таймер";
        }
    }

    private void UpdateTrayIcon(TimeSpan remaining)
    {
        _trayIconService.UpdateTimerStatus(_viewModel.IsRunning, remaining);
    }
}
