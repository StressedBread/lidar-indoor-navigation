using LidarIndoorNavigation.Models;
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
        int lessSectors = 3;

        int sideSectorSizeSteps = 0;
        int middleSectorSizeSteps = 0;

        int lessSectorsSizeSteps = 0;

        int safeDistanceSide = 400;
        int safeDistanceMiddle = 600;

        internal ReactiveNavigation()
        {
            sideSectorSizeSteps = (end_step - start_step) / sectors;
            middleSectorSizeSteps = sideSectorSizeSteps + 2;

            lessSectorsSizeSteps = (end_step - start_step) / sectors;
        }

        public (int RB, int RF, int Mid, int LF, int LB) CalculateMinDistance()
        {
            int currentSectorRB = 99999;
            int currentSectorRF = 99999;
            int currentSectorMid = 99999;
            int currentSectorLF = 99999;
            int currentSectorLB = 99999;

            for (int i = 0; i < DistancePointsStaticList.Distances.Count; i++)
            {
                if (DistancePointsStaticList.Distances[i] <= 20 || DistancePointsStaticList.Distances[i] > 4600)
                {
                    continue;
                }

                else if (i >= 0 && i <= sideSectorSizeSteps)
                {
                    if (currentSectorRB > (int)DistancePointsStaticList.Distances[i])
                        currentSectorRB = (int)DistancePointsStaticList.Distances[i];
                }

                else if (i >= sideSectorSizeSteps && i <= 2 * sideSectorSizeSteps)
                {
                    if (currentSectorRF > (int)DistancePointsStaticList.Distances[i])
                        currentSectorRF = (int)DistancePointsStaticList.Distances[i];
                }

                else if (i >= 2 * sideSectorSizeSteps && i <= 2 * sideSectorSizeSteps + middleSectorSizeSteps)
                {
                    if (currentSectorMid > (int)DistancePointsStaticList.Distances[i])
                        currentSectorMid = (int)DistancePointsStaticList.Distances[i];
                }

                else if (i >= DistancePointsStaticList.Distances.Count - (2 * sideSectorSizeSteps) && i <= DistancePointsStaticList.Distances.Count - sideSectorSizeSteps)
                {
                    if (currentSectorLF > (int)DistancePointsStaticList.Distances[i])
                        currentSectorLF = (int)DistancePointsStaticList.Distances[i];
                }

                else if (i >= DistancePointsStaticList.Distances.Count - sideSectorSizeSteps && i <= DistancePointsStaticList.Distances.Count)
                {
                    if (currentSectorLB > (int)DistancePointsStaticList.Distances[i])
                        currentSectorLB = (int)DistancePointsStaticList.Distances[i];
                }
            }

            return (currentSectorRB, currentSectorRF, currentSectorMid, currentSectorLF, currentSectorLB);
        }

        public (int R, int Mid, int L) CalculateMinDistanceLessSectors()
        {
            int currentSectorR = 99999;
            int currentSectorMid = 99999;
            int currentSectorL = 99999;

            for (int i = 0; i < DistancePointsStaticList.Distances.Count; i++)
            {
                if (DistancePointsStaticList.Distances[i] <= 20 || DistancePointsStaticList.Distances[i] > 4600)
                {
                    continue;
                }

                else if (i >= 0 && i <= lessSectorsSizeSteps)
                {
                    if (currentSectorR > (int)DistancePointsStaticList.Distances[i])
                        currentSectorR = (int)DistancePointsStaticList.Distances[i];
                }

                else if (i >= lessSectorsSizeSteps && i <= 2 * sideSectorSizeSteps)
                {
                    if (currentSectorMid > (int)DistancePointsStaticList.Distances[i])
                        currentSectorMid = (int)DistancePointsStaticList.Distances[i];
                }

                else if (i >= 2 * sideSectorSizeSteps && i <= end_step - lessSectorsSizeSteps)
                {
                    if (currentSectorL > (int)DistancePointsStaticList.Distances[i])
                        currentSectorL = (int)DistancePointsStaticList.Distances[i];
                }
            }

            return (currentSectorR, currentSectorMid, currentSectorL);
        }

        public void DecisionLogic(int RB, int RF, int Mid, int LF, int LB)
        {

        }

        public MovementCommands DecisionLogicLessSectors((int R, int Mid, int L) sectors)
        {
            if (sectors.Mid < safeDistanceMiddle)
            {
                if (sectors.R < safeDistanceSide && sectors.L < safeDistanceSide)
                {
                    return MovementCommands.Stop;
                }
                else if (sectors.R < safeDistanceSide)
                {
                    return MovementCommands.TurnLeft;
                }
                else if (sectors.L < safeDistanceSide)
                {
                    return MovementCommands.TurnRight;
                }

                else
                {
                    return MovementCommands.Stop;
                }
            }
            else
            {
                return MovementCommands.Forward;
            }
            
        }
    }
}
