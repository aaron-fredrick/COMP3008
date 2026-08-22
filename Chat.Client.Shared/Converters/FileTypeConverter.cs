using System;
using System.Globalization;
using System.Windows.Data;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Shared.Converters
{
    public class FileTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileType fileType)
            {
                return fileType.ToString().ToUpper();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
