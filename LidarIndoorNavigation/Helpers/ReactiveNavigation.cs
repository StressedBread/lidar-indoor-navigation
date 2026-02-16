using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    internal class ReactiveNavigation
    {
        const int start_step = 44;
        const int end_step = 725;
        const double stepAngle = 0.3515625;

        int sectors = 5;
        int sideSectorSizeSteps = 0;
        int middleSectorSizeSteps = 0;
        int safeDistanceSide = 400;
        int safeDistanceMiddle = 600;

        List<long> sectorRB = new();
        List<long> sectorRF = new(); 
        List<long> sectorMid = new(); 
        List<long> sectorLF = new(); 
        List<long> sectorLB = new();

        internal ReactiveNavigation()
        {
            sideSectorSizeSteps = (end_step - start_step) / sectors;
            middleSectorSizeSteps = sideSectorSizeSteps + 2;
        }
    }
}
