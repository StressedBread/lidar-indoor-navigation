using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    internal class PolarToCartesianConverter
    {
        const int start_step = 44;
        const double stepAngle = 0.3515625;

        List<(double x, double y)> cartesianPoints = new List<(double x, double y)>();
        public List<(double x, double y)> ConvertToCartesian(List<long> distances)
        {
            cartesianPoints.Clear();
            for (int i = 0; i < distances.Count; ++i)
            {
                if (distances[i] <= 400 || distances[i] > 4600)
                {
                    continue;
                }

                double angle = -(start_step + i - 384) * stepAngle;
                double x = distances[i] * Math.Sin(angle * Math.PI / 180);
                double y = distances[i] * Math.Cos(angle * Math.PI / 180);
                cartesianPoints.Add((x, y));
            }
            return cartesianPoints;
        }
    }
}
