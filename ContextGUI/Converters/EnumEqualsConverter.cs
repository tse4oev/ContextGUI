using System;
using System.Globalization;
using System.Windows.Data;

namespace ContextGUI.Converters;

public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter == null || value is not bool boolValue || !boolValue)
        {
            return Binding.DoNothing;
        }

        return Enum.Parse(targetType, parameter.ToString()!, ignoreCase: true);
    }
}
