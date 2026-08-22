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
            double r = 12; // Leave a 2px margin

            // Entry angle
            double angle1 = rand.NextDouble() * 2 * Math.PI;
            // Exit angle: at least 45 degrees (0.78 rad) apart, up to 315 degrees
            double angle2 = angle1 + 0.8 + (rand.NextDouble() * 4.6);

            // Entry and exit points on the circle
            double px1 = cx + r * Math.Cos(angle1);
            double py1 = cy + r * Math.Sin(angle1);
            double px2 = cx + r * Math.Cos(angle2);
            double py2 = cy + r * Math.Sin(angle2);

            // To create a loop, control points shoot across the center
            double cp1Angle = angle1 + Math.PI + (rand.NextDouble() - 0.5);
            double cp2Angle = angle2 + Math.PI + (rand.NextDouble() - 0.5);

            // Length of control vectors (long enough to cross and loop)
            double cp1Len = rand.NextDouble() * 12 + 16;
            double cp2Len = rand.NextDouble() * 12 + 16;

            double cp1x = px1 + cp1Len * Math.Cos(cp1Angle);
            double cp1y = py1 + cp1Len * Math.Sin(cp1Angle);
            double cp2x = px2 + cp2Len * Math.Cos(cp2Angle);
            double cp2y = py2 + cp2Len * Math.Sin(cp2Angle);

            string path = FormattableString.Invariant($"M {px1:F1},{py1:F1} C {cp1x:F1},{cp1y:F1} {cp2x:F1},{cp2y:F1} {px2:F1},{py2:F1}");
            
            return path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
