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
        const double frontRiskThreshold = 1;
        const double softThreshold = 0.6;
        const double deadZone = 20;
        const int hold = 1;

        double lastMoveAngle = 0;
        int turnCommit = 0;
        double committedAngle = 0;

        double leftRightCounter = 0;
        bool isTurning = false;
        int turnFrames = 0;
        int turnMiliSeconds = 2000;
        double turnDirection = 0;

        ICP icp = new();

        RiskCalculation riskCalculation = new();
        private double[] risks = new double[sectors];
        public double moveAngle = 0;
        private double forwardScale = 1;
        private bool isBlocked = false;

        private MovementCommands lastCommad = MovementCommands.Stop;
        private int holdCounter = 0;


        public (double moveAngle, double[] risks, double frontRisk) DecideMovement(List<(double x, double y)> cleanScan)
        {
            /*icp.Update(cleanScan);

            if (WaypointNavigator.goalReached)
            {
                icp.Reset();
            }*/

            risks = riskCalculation.EvaluateSectors();
            
            System.Diagnostics.Debug.WriteLine(string.Join(", ", risks.Select(r => r.ToString("F2"))));

            if (isTurning)
            {
                /*moveAngle = turnDirection;
                turnFrames--;
                System.Diagnostics.Debug.WriteLine("Turn Frames: " + turnFrames);

                if (turnFrames <= 0)
                    isTurning = false;*/

                Thread.Sleep(turnMiliSeconds);
                leftRightCounter = 0;

                return (moveAngle, risks, frontRisk: 0);
            }

            double totalX = 0, totalY = 0;

            for (int i = 0; i < sectors; i++)
            {
                double angle = (-120 + i * sectorWidth + sectorWidth / 2) * Math.PI / 180;
                double weight = Math.Max(0, 1 - risks[i]);
                totalX += -Math.Sin(angle) * weight;
                totalY += Math.Cos(angle) * weight;
            }

            double magnitude = Math.Sqrt(totalX * totalX + totalY * totalY);
            moveAngle = Math.Atan2(totalX, totalY) * 180 / Math.PI;
            System.Diagnostics.Debug.WriteLine("Angle: " + moveAngle);
            isBlocked = magnitude < 1;

            int mid = (sectors / 2) - 1;
            double frontRisk = (risks[mid + 1] + risks[mid]) / 2;
            System.Diagnostics.Debug.WriteLine("Front risk: " + frontRisk);
            forwardScale = Math.Max(0, 1 - frontRisk / 1.5);

            if (leftRightCounter >= 3)
            {
                double leftRisk = risks.Skip(mid + 1).Sum();
                double rightRisk = risks.Take(mid).Sum();
                moveAngle = leftRisk < rightRisk ? 30 : -30;
                isTurning = true;

            }
            else (!isBlocked && frontRisk > frontRiskThreshold && Math.Abs(moveAngle) < deadZone)
            {
                double leftRisk = risks.Skip(mid + 1).Sum();
                double rightRisk = risks.Take(mid).Sum();
                moveAngle = leftRisk < rightRisk ? 30 : -30;
                leftRightCounter += 0.5;
            }

            /*if (turnCommit > 0)
            {
                moveAngle = committedAngle;
                turnCommit--;
            }
            else if (!isBlocked && frontRisk > frontRiskThreshold)
            {
                double leftRisk = risks.Skip(mid + 1).Sum();
                double rightRisk = risks.Take(mid).Sum();
                committedAngle = leftRisk < rightRisk ? 30 : -30;
                turnCommit = 5;
                moveAngle = committedAngle;
            }*/

            /*if (frontRisk > softThreshold && !isTurning)
            {
                double leftRisk = risks.Skip(mid + 1).Sum();
                double rightRisk = risks.Take(mid).Sum();

                turnDirection = leftRisk < rightRisk ? 30 : -30;

                isTurning = true;
                turnFrames = 18; // VERY important tuning parameter

                moveAngle = turnDirection;
            }*/

            /*moveAngle = 0.7 * lastMoveAngle + 0.3 * moveAngle;
            lastMoveAngle = moveAngle;*/

            /*double? goalAngle = WaypointNavigator.GetSteeringAgle(icp.positionX, icp.positionY, icp.heading, forwardScale);

            if (goalAngle.HasValue)
            {
                double goalWeight = forwardScale;
                double avoidanceWeight = 1 - forwardScale;
                moveAngle = goalAngle.Value * goalWeight + moveAngle * avoidanceWeight;
            }
            
            if (goalAngle != null) return goalAngle.Value;*/
            return (moveAngle, risks, frontRisk);
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
