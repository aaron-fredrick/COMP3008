using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Chat.Client.Shared.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string seedStr = value as string;
            if (string.IsNullOrEmpty(seedStr)) seedStr = "default";
            
            int hash = 0;
            foreach (char c in seedStr)
            {
                hash = (hash * 31) + c;
            }
            Random rand = new Random(hash);

            // Generate HSL color for vibrant, pleasing colors (saturation 60-90%, lightness 55-75%)
            double h = rand.NextDouble() * 360;
            double s = rand.NextDouble() * 0.3 + 0.6; 
            double l = rand.NextDouble() * 0.2 + 0.55; 

            Color color = HslToRgb(h, s, l);
            return new SolidColorBrush(color);
        }

        private Color HslToRgb(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;

            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
