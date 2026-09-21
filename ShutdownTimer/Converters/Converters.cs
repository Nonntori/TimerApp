using System;
using System.Globalization;
using System.Windows.Data;

namespace ShutdownTimer.Converters;

/// <summary>
/// Converts TimeSpan to HH:MM:SS format string
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return $"{Math.Max(0, (int)timeSpan.TotalHours):D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        return "00:00:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts boolean value
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}

/// <summary>
/// Converts progress value (0-1) to arc angle
/// </summary>
public class ProgressToAngleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            // Full circle is 360 degrees, but we want to start from top (-90)
            return Math.Max(0, progress * 360);
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Multi-value converter for action display
/// </summary>
public class ActionToTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is Models.ShutdownAction action)
        {
            return action switch
            {
                Models.ShutdownAction.Shutdown => "Выключить компьютер",
                Models.ShutdownAction.Restart => "Перезагрузить компьютер",
                Models.ShutdownAction.LogOff => "Выйти из системы",
                Models.ShutdownAction.Sleep => "Спящий режим",
                _ => "Действие"
            };
        }
        return "Выберите действие";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
