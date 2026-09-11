using System;
using System.Globalization;
using System.Windows.Data;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Shared.Converters
{
    public class MessageTypeToFileTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageType messageType)
            {
                return "FILE";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
