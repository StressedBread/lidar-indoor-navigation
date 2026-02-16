using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    public static class DistancePointsStaticList
    {
        public static List<long> Distances { get; } = new();

        public static void Clear()
        {
            Distances.Clear();
        }
    }
}
