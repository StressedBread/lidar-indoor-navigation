using LidarIndoorNavigation.Models;
using System.Diagnostics;

namespace LidarIndoorNavigation.Helpers
{
    internal class ReactiveNavigation
    {
        const double span = 240;
        const double deadZone = 20;
        const int hold = 1;

        private double[]? risks;
        private int lastSectorCount = -1;
        double leftRightCounter = 0;
        bool isTurning = false;
        int turnMiliSeconds = 300;

        private Stopwatch turnStopwatch = new();

        RiskCalculation riskCalculation = new();
        public double moveAngle = 0;
        private double forwardScale = 1;
        private bool isBlocked = false;

        private MovementCommands lastCommad = MovementCommands.Stop;
        private int holdCounter = 0;


        public (double moveAngle, double[] risks, double frontRisk) DecideMovement(int distanceCells, int frontRiskThreshold, int sectors)
        {
            double sectorWidth = span / sectors;
            if (risks == null || lastSectorCount != sectors)
            {
                risks = new double[sectors];
                lastSectorCount = sectors;
            }

            risks = riskCalculation.EvaluateSectors(distanceCells, sectors);

            if (isTurning && leftRightCounter >= 3)
            {
                if (turnStopwatch.ElapsedMilliseconds >= turnMiliSeconds)
                {
                    isTurning = false;
                    leftRightCounter = 0;
                }

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
            isBlocked = magnitude < 1;

            int mid = (sectors / 2) - 1;
            double frontRisk = (risks[mid + 1] + risks[mid]) / 2;
            forwardScale = Math.Max(0, 1 - frontRisk / 1.5);

            if (leftRightCounter >= 3)
            {
                double leftRisk = risks.Skip(mid + 1).Sum();
                double rightRisk = risks.Take(mid).Sum();
                moveAngle = leftRisk < rightRisk ? 30 : -30;
                isTurning = true;
                turnStopwatch.Restart();
            }
            else if (!isBlocked && frontRisk > frontRiskThreshold && Math.Abs(moveAngle) < deadZone)
            {
                double leftRisk = risks.Skip(mid + 1).Sum();
                double rightRisk = risks.Take(mid).Sum();
                moveAngle = leftRisk < rightRisk ? 30 : -30;
                leftRightCounter += 0.5;
            }
           
           return (moveAngle, risks, frontRisk);
        }

        public (MovementCommands command, double forwardScale) GetCommand(double finalMoveAngle)
        {
            MovementCommands raw;

            if (finalMoveAngle < -deadZone)
                raw = MovementCommands.TurnLeft;
            else if (finalMoveAngle > deadZone)
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
