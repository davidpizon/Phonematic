using System.Globalization;
using Avalonia.Data.Converters;

namespace Transcriptonator.Converters;

public class PercentageConverter : IValueConverter
{
    public static readonly PercentageConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{d:P0}";
        return "0%";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class FileSizeConverter : IValueConverter
{
    public static readonly FileSizeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1048576 => $"{bytes / 1024.0:F1} KB",
                < 1073741824 => $"{bytes / 1048576.0:F1} MB",
                _ => $"{bytes / 1073741824.0:F2} GB"
            };
        }
        return "0 B";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class DurationConverter : IValueConverter
{
    public static readonly DurationConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s"
                : ts.TotalMinutes >= 1
                    ? $"{ts.Minutes}m {ts.Seconds}s"
                    : $"{ts.Seconds}s";
        }
        return "0s";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
