using System;
using System.Globalization;
using System.Windows.Data;

namespace Chat.Client.Shared.Converters
{
    public class FileNameExtractorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string content && !string.IsNullOrEmpty(content))
            {
                if (content.StartsWith("Shared file: "))
                {
                    return content.Replace("Shared file: ", "");
                }
                return content;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
