using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    internal class ICP
    {
        private List<(double x, double y)> previousScan = new();

        public double positionX { get; private set; } = 0;
        public double positionY { get; private set; } = 0;
        public double heading { get; private set; } = 0;

        private double matchDistance = 150;
        private double rotationMatchDistance = 100;
        private int downSampleStep = 4;

        internal void Update(List<(double x, double y)> currentScan)
        {
            if (currentScan.Count == 0) return;

            List<(double x, double y)> cleanScan = currentScan.Where((_, i) => i < DistancePointsStaticList.Distances.Count && DistancePointsStaticList.Distances[i] > 20 && DistancePointsStaticList.Distances[i] < 4600).ToList();

            List<(double x, double y)> downSampledCurrent = cleanScan.Where((_, i) => i % downSampleStep == 0).ToList();

            if (previousScan.Count == 0)
            {
                previousScan = downSampledCurrent;
                return;
            }

            (double deltaX, double deltaY, double deltaT) estimatedMotion = EstimateMotion(previousScan, downSampledCurrent);

            double headingRad = heading * Math.PI / 180;
            positionX += Math.Cos(headingRad) * estimatedMotion.deltaX - Math.Sin(headingRad) * estimatedMotion.deltaY;
            positionY += Math.Sin(headingRad) * estimatedMotion.deltaX + Math.Cos(headingRad) * estimatedMotion.deltaY;
            heading += estimatedMotion.deltaT * 180 / Math.PI;

            while (heading > 180) heading -= 360;
            while (heading < -180) heading += 360;

            previousScan = downSampledCurrent;
        }

        internal void Reset()
        {
            positionX = 0;
            positionY = 0;
            heading = 0;
            previousScan.Clear();
        }

        private (double deltaX, double deltaY, double deltaT) EstimateMotion(List<(double x, double y)> previousPoints, List<(double x, double y)> currentPoints)
        {
            double sumDeltaX = 0;
            double sumDeltaY = 0;
            int matchCount = 0;

            foreach (var current in currentPoints)
            {
                (double x, double y) closestPoint = previousPoints.MinBy(p => GetDistance(p, current));
                double pointDistance = GetDistance(closestPoint, current);

                if (pointDistance < matchDistance)
                {
                    sumDeltaX += current.x - closestPoint.x;
                    sumDeltaY += current.y - closestPoint.y;
                    matchCount++;
                }
            }

            if (matchCount == 0) return (0, 0, 0);

            double deltaX = sumDeltaX / matchCount;
            double deltaY = sumDeltaY / matchCount;
            double deltaT = EstimateRotation(previousPoints, currentPoints, deltaX, deltaY);

            return (deltaX, deltaY, deltaT);
        }

        private double EstimateRotation(List<(double x, double y)> previousPoints, List<(double x, double y)> currentPoints, double deltaX, double deltaY)
        {
            double sumAngleDiff = 0;
            int matchCount = 0;

            foreach (var current in currentPoints)
            {
                (double x, double y) translatedPoint = (current.x - deltaX, current.y - deltaY);
                (double x, double y) closestPoint = previousPoints.MinBy(p => GetDistance(p, translatedPoint));

                if (GetDistance(closestPoint, translatedPoint) < rotationMatchDistance)
                {
                    double previousAngle = Math.Atan2(closestPoint.y, closestPoint.x);
                    double translatedAngle = Math.Atan2(translatedPoint.y, translatedPoint.x);
                    double angleDiff = translatedAngle - previousAngle;

                    while (angleDiff > Math.PI) angleDiff -= 2 * Math.PI;
                    while (angleDiff < -Math.PI) angleDiff += 2 * Math.PI;

                    sumAngleDiff += angleDiff;
                    matchCount++;
                }
            }

            return matchCount == 0 ? 0 : sumAngleDiff / matchCount;
        }

        internal double GetDistance((double x, double y) pointA, (double x, double y) pointB)
        {
            double deltaX = pointA.x - pointB.x;
            double deltaY = pointA.y - pointB.y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
}
