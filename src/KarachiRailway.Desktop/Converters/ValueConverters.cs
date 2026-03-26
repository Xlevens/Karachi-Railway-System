using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace KarachiRailway.Desktop.Converters;

/// <summary>Converts bool to Visibility (True → Visible, False → Collapsed).</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Converts bool to Visibility, inverted (True → Collapsed, False → Visible).</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

/// <summary>Converts a double (0-1) to a percentage string, e.g. 0.8 → "80.0%".</summary>
[ValueConversion(typeof(double), typeof(string))]
public sealed class DoubleToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double d ? $"{d * 100:F1}%" : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts double to string with 2 decimal places; NaN → "—".</summary>
[ValueConversion(typeof(double), typeof(string))]
public sealed class DoubleToFixedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return double.IsNaN(d) ? "—" : d.ToString("F2", culture);
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts a hex color string to a SolidColorBrush.</summary>
[ValueConversion(typeof(string), typeof(SolidColorBrush))]
public sealed class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch { /* fall through */ }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts utilisation (ρ) to a status color brush:
/// ≤ 0.7 → green, ≤ 0.9 → amber, else → red.
/// </summary>
[ValueConversion(typeof(double), typeof(SolidColorBrush))]
public sealed class UtilizationToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double rho)
        {
            if (double.IsNaN(rho) || rho >= 1.0)
                return new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)); // red
            if (rho >= 0.9)
                return new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)); // amber
            return new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));      // green
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Inverts a bool value (true → false, false → true).</summary>
[ValueConversion(typeof(bool), typeof(bool))]
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is false;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is false;
}
