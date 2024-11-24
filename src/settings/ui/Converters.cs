using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;

namespace KLPlugins.DynLeaderboards.Settings.UI;

[ValueConversion(typeof(string), typeof(SolidColorBrush))]
public class StringToSolidColorBrushConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value == null || value is not string str) {
            return DependencyProperty.UnsetValue;
        }

        return new SolidColorBrush(WindowsMediaColorExtensions.FromHex(str));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}

[ValueConversion(typeof(string), typeof(Color))]
public class StringToColorConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value == null || value is not string str) {
            return DependencyProperty.UnsetValue;
        }

        return WindowsMediaColorExtensions.FromHex(str);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value == null || value is not Color) {
            throw new ArgumentException("value must be Color");
        }

        var c = (Color)value;
        return c.ToString();
    }
}

public class BoolToTConverter<T>(T trueValue, T falseValue) : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not bool val) {
            return DependencyProperty.UnsetValue;
        }

        if (parameter is string s) {
            var lower = s.ToLower();
            if (lower == "rev" || lower == "reverse") {
                if (val) {
                    return falseValue;
                }

                return trueValue;
            }
        }

        if (val) {
            return trueValue;
        }

        return falseValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}

[ValueConversion(typeof(bool), typeof(double))]
public class IsEnabledToOpacityConverter() : BoolToTConverter<double>(1.0, SettingsControl.DISABLED_OPTION_OPACITY);

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityCollapsedConverter() : BoolToTConverter<Visibility>(
    Visibility.Visible,
    Visibility.Collapsed
);

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityHiddenConverter() : BoolToTConverter<Visibility>(Visibility.Visible, Visibility.Hidden);

[ValueConversion(typeof(CarClass?), typeof(string))]
public class StringCarClassConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value == null) {
            return null;
        }

        if (value is not CarClass) {
            return DependencyProperty.UnsetValue;
        }

        var c = (CarClass)value;
        return c.AsString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value == null) {
            return null;
        }

        if (value is not string str) {
            throw new ArgumentException("value must be string");
        }

        return new CarClass(str);
    }
}

[ValueConversion(typeof(object), typeof(bool))]
public class IsNull : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}

[ValueConversion(typeof(object), typeof(bool))]
public class IsNotNull : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}

[ValueConversion(typeof(object), typeof(object))]
public class NullToUnsetValueConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value ?? DependencyProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value == DependencyProperty.UnsetValue ? null : value;
    }
}