using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    internal class TestDataGenerator
    {
        Random random = new Random();

        List<long> distances = new();
        List<(double x, double y)> cartesianDistances = new();

        PolarToCartesianConverter cartesianConverter = new PolarToCartesianConverter();

        public void GenerateTestData()
        {
            for (int i = 0; i < 682; i++)
            {
                distances.Add(random.Next(20, 4600));
            }

            DistancePointsStaticList.Clear();
            DistancePointsStaticList.Distances.AddRange(distances);

            cartesianDistances = cartesianConverter.ConvertToCartesian(DistancePointsStaticList.Distances);

            DistancePointsStaticList.CartesianClear();
            DistancePointsStaticList.CartesianDistances.AddRange(cartesianDistances);
        }
    }
}
