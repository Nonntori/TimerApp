using System.Globalization;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

/// <summary>
/// Service for managing application settings
/// </summary>
public class SettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appData, "ShutdownTimer");
        
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }
        
        _settingsPath = Path.Combine(appDir, "settings.json");
        _settings = LoadSettings();
    }

    public AppSettings Settings => _settings;

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to load settings: {ex.Message}");
        }
        return new AppSettings();
    }

    public void SaveSettings()
    {
        try
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            LogError($"Failed to save settings: {ex.Message}");
        }
    }

    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        updateAction(_settings);
        SaveSettings();
    }

    private void LogError(string message)
    {
        var logPath = GetLogPath();
        try
        {
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}{Environment.NewLine}");
        }
        catch { }
    }

    private string GetLogPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "ShutdownTimer", "logs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
    }
}
