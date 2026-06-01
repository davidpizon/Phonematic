using Avalonia.Data.Converters;
using System.Globalization;

namespace Phonematic.Converters;

/// <summary>
/// Converts a <see cref="double"/> progress value in the range 0.0–1.0 to a
/// whole-number percentage string (e.g. <c>0.753</c> → <c>"75%"</c>).
/// One-way only; <c>ConvertBack</c> throws <see cref="NotSupportedException"/>.
/// </summary>
public class PercentageConverter : IValueConverter
{
    /// <summary>Shared singleton instance for use in XAML resources.</summary>
    public static readonly PercentageConverter Instance = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{d * 100:0}%";
        return "0%";
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a <see cref="long"/> byte count to a human-readable file-size string
/// (e.g. <c>1_572_864L</c> → <c>"1.5 MB"</c>). Thresholds: B / KB / MB / GB.
/// One-way only; <c>ConvertBack</c> throws <see cref="NotSupportedException"/>.
/// </summary>
public class FileSizeConverter : IValueConverter
{
    /// <summary>Shared singleton instance for use in XAML resources.</summary>
    public static readonly FileSizeConverter Instance = new();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a <see cref="double"/> seconds value to a human-readable duration string
/// (e.g. <c>3725.0</c> → <c>"1h 2m 5s"</c>). Shows hours only when ≥ 1 hour.
/// One-way only; <c>ConvertBack</c> throws <see cref="NotSupportedException"/>.
/// </summary>
public class DurationConverter : IValueConverter
{
    /// <summary>Shared singleton instance for use in XAML resources.</summary>
    public static readonly DurationConverter Instance = new();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Inverts a <see cref="bool"/> value (e.g. binds a "disabled" property to an "is-loading" flag).
/// Both <c>Convert</c> and <c>ConvertBack</c> return <c>!value</c>; non-bool values are
/// returned unchanged.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    /// <summary>Shared singleton instance for use in XAML resources.</summary>
    public static readonly InverseBoolConverter Instance = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
