using System;
using System.Globalization;
using System.Windows.Data;

namespace Chat.Client.Shared.Converters
{
    public class StringToRibbonPathConverter : IValueConverter
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

            double cx = 14;
            double cy = 14;
            double r = 13; // Entry/exit near the edge

            // Base angle for the entire shape
            double baseAngle = rand.NextDouble() * 2 * Math.PI;
            
            // Randomize the loop's spread and size slightly
            double spread = 0.5 + rand.NextDouble() * 0.4; // 0.5 to 0.9 radians
            double cpSpread = 2.0 + rand.NextDouble() * 0.8; // 2.0 to 2.8 radians
            double cpDist = 18 + rand.NextDouble() * 6; // 18 to 24

            double a1 = baseAngle - spread;
            double a2 = baseAngle + spread;
            double a3 = baseAngle + cpSpread; // CP1 crosses to the opposite side
            double a4 = baseAngle - cpSpread; // CP2 crosses to the opposite side

            double px1 = cx + r * Math.Cos(a1);
            double py1 = cy + r * Math.Sin(a1);
            double px2 = cx + r * Math.Cos(a2);
            double py2 = cy + r * Math.Sin(a2);

            double cp1x = cx + cpDist * Math.Cos(a3);
            double cp1y = cy + cpDist * Math.Sin(a3);
            double cp2x = cx + cpDist * Math.Cos(a4);
            double cp2y = cy + cpDist * Math.Sin(a4);

            string path = FormattableString.Invariant($"M {px1:F1},{py1:F1} C {cp1x:F1},{cp1y:F1} {cp2x:F1},{cp2y:F1} {px2:F1},{py2:F1}");
            
            return path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
