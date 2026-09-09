using System;
using System.Globalization;
using System.Windows.Data;

namespace Chat.Client.Shared.Converters
{
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrEmpty(name))
            {
                string[] parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return $"{parts[0][0]}{parts[1][0]}";
                }
                return name.Length >= 2 ? name.Substring(0, 2) : name;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
