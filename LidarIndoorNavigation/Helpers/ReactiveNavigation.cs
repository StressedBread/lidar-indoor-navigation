using LidarIndoorNavigation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LidarIndoorNavigation.Helpers
{
    internal class ReactiveNavigation
    {
        const int sectors = 20;
        const double span = 240;
        const double sectorWidth = span / sectors;
        const double frontRiskThreshold = 0.25;
        const double deadZone = 15;
        const int hold = 1;

        ICP icp = new();

        RiskCalculation riskCalculation = new();
        private double[] risks = new double[sectors];
        public double moveAngle = 0;
        private double forwardScale = 1;
        private bool isBlocked = false;

        private MovementCommands lastCommad = MovementCommands.Stop;
        private int holdCounter = 0;

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

        public double DecideMovement(List<(double x, double y)> cleanScan)
        {
            /*icp.Update(cleanScan);

            if (WaypointNavigator.goalReached)
            {
                icp.Reset();
            }*/

            risks = riskCalculation.EvaluateSectors();

            double totalX = 0, totalY = 0;

            for (int i = 0; i < sectors; i++)
            {
                double angle = (-120 + i * sectorWidth + sectorWidth / 2) * Math.PI / 180;
                double weight = Math.Max(0, 1 - risks[i]);
                totalX += Math.Sin(angle) * weight;
                totalY += Math.Cos(angle) * weight;
            }

            double magnitude = Math.Sqrt(totalX * totalX + totalY * totalY);
            moveAngle = Math.Atan2(totalX, totalY) * 180 / Math.PI;
            isBlocked = magnitude < 0.3;

            int mid = (sectors / 2) - 1;
            double frontRisk = (risks[mid + 1] + risks[mid]) / 2;
            forwardScale = Math.Max(0, 1 - frontRisk / 1.5);

            if (!isBlocked && frontRisk > frontRiskThreshold && Math.Abs(moveAngle) < deadZone)
            {
                double leftRisk = risks.Skip(mid + 1).Sum();
                double rightRisk = risks.Take(mid).Sum();
                moveAngle = leftRisk < rightRisk ? 30 : -30;
            }

            /*double? goalAngle = WaypointNavigator.GetSteeringAgle(icp.positionX, icp.positionY, icp.heading, forwardScale);

            if (goalAngle.HasValue)
            {
                double goalWeight = forwardScale;
                double avoidanceWeight = 1 - forwardScale;
                moveAngle = goalAngle.Value * goalWeight + moveAngle * avoidanceWeight;
            }
            
            if (goalAngle != null) return goalAngle.Value;*/
            return moveAngle;
        }

        public (MovementCommands command, double forwardScale) GetCommand(double finalMoveAngle)
        {
            MovementCommands raw;

            if (finalMoveAngle > deadZone)
                raw = MovementCommands.TurnLeft;
            else if (finalMoveAngle < -deadZone)
                raw = MovementCommands.TurnRight;
            else
                raw = MovementCommands.Forward;

            return (Stabilize(raw), forwardScale);
        }

        private MovementCommands Stabilize(MovementCommands newCommand)
        {
            if (isBlocked)
            {
                holdCounter = 0;
                lastCommad = MovementCommands.Stop;
                return lastCommad;
            }

            if (newCommand == lastCommad)
            {
                holdCounter = 0;
                return lastCommad;
            }
            holdCounter++;

            if (holdCounter >= hold)
            {
                lastCommad = newCommand;
                holdCounter = 0;
            }

            return lastCommad;
        }
    }
}
