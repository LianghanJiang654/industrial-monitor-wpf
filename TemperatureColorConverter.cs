using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FactorialApp
{
    public class TemperatureColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string text && int.TryParse(text, out int number))
            {
                int threshold = 26;
                if (parameter is string thresholdText && int.TryParse(thresholdText, out int parsedThreshold))
                {
                    threshold = parsedThreshold;
                }

                if (number > threshold)
                {
                    return Brushes.Red;
                }
            }
            return Brushes.White;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}