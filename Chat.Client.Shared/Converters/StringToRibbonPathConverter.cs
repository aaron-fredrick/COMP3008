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
            
            // A ribbon path needs interesting twists. 
            // We'll generate 4 to 6 anchor points around the circle.
            int points = rand.Next(4, 7);
            string path = "";
            
            double[] px = new double[points];
            double[] py = new double[points];
            for (int i = 0; i < points; i++)
            {
                // spread points around the circle with some randomness
                double angle = (2 * Math.PI / points) * i + (rand.NextDouble() - 0.5) * 0.5;
                // random radius to keep it inside a 28x28 box (center 14,14), max radius 12
                double r = rand.NextDouble() * 8 + 4; 
                px[i] = cx + r * Math.Cos(angle);
                py[i] = cy + r * Math.Sin(angle);
            }
            
            path += $"M {px[0]:F1},{py[0]:F1} ";
            
            for (int i = 0; i < points; i++)
            {
                int next = (i + 1) % points;
                
                // create control points for a cubic bezier curve to make it loop and twist
                double cp1x = px[i] + (rand.NextDouble() - 0.5) * 16;
                double cp1y = py[i] + (rand.NextDouble() - 0.5) * 16;
                double cp2x = px[next] + (rand.NextDouble() - 0.5) * 16;
                double cp2y = py[next] + (rand.NextDouble() - 0.5) * 16;
                
                path += $"C {cp1x:F1},{cp1y:F1} {cp2x:F1},{cp2y:F1} {px[next]:F1},{py[next]:F1} ";
            }
            
            return path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
