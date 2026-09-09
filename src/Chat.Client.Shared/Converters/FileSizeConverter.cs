using System;
using System.Globalization;
using System.Windows.Data;

namespace Chat.Client.Shared.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                if (bytes < 1024)
                {
                    return $"{bytes} B";
                }
                else if (bytes < 1024 * 1024)
                {
                    double kb = bytes / 1024.0;
                    return $"{Math.Round(kb)} kB";
                }
                else
                {
                    double mb = bytes / (1024.0 * 1024.0);
                    return $"{Math.Round(mb)} MB";
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
