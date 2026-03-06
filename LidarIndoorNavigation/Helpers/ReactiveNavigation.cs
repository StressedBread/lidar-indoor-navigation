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

        int bestSector = 0;

        private float[] risks;
        private double moveAngle = 0;

        private int currentHeading = 0;
        private int nextHeading = 0;

        RiskCalculation riskCalculation = new();

        internal ReactiveNavigation()
        {
            /*sideSectorSizeSteps = (end_step - start_step) / sectors;
            middleSectorSizeSteps = sideSectorSizeSteps + 2;

            lessSectorsSizeSteps = (end_step - start_step) / lessSectors;*/
        }

        /*public (int R, int Mid, int L) CalculateMinDistanceLessSectors()
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

                else if (i >= lessSectorsSizeSteps && i <= 2 * lessSectorsSizeSteps)
                {
                    if (currentSectorMid > (int)DistancePointsStaticList.Distances[i])
                        currentSectorMid = (int)DistancePointsStaticList.Distances[i];
                }

                else if (i >= 2 * lessSectorsSizeSteps && i <=  3 * lessSectorsSizeSteps)
                {
                    if (currentSectorL > (int)DistancePointsStaticList.Distances[i])
                        currentSectorL = (int)DistancePointsStaticList.Distances[i];
                }
            }

            return (currentSectorR, currentSectorMid, currentSectorL);
        }

        public MovementCommands DecisionLogicLessSectors((int R, int Mid, int L) sectors)
        {
            if (sectors.Mid < safeDistanceMiddle)
            {
                if (sectors.R < safeDistanceSide)
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
        }*/

        public void DecideMovement()
        {
            risks = riskCalculation.EvaluateSectors();
            bestSector = Array.IndexOf(risks, risks.Min());

            moveAngle = -120 + bestSector * (240 / risks.Length) + (240 / risks.Length) / 2;


        }
    }
}
