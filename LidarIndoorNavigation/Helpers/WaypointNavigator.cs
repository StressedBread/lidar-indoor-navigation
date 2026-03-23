using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    public static class WaypointNavigator
    {
        private static double arrivalRadius = 20;
        private static double obstacleHandoff = 0.5;

        private static double goalX = 0;
        private static double goalY = 0;
        private static bool goalSet = false;

        public static bool goalReached = false;

        public static void SetGoal(double x, double y)
        {
            goalX = x;
            goalY = y;
            goalSet = true;
            goalReached = false;
        }

        public static double? GetSteeringAgle(double robotX, double robotY, double robotHeading, double forwardScale)
        {
            if (!goalSet || goalReached) return null;

            double distanceToGoal = GetDistanceToGoal(robotX, robotY);

            if (distanceToGoal < arrivalRadius)
            {
                goalReached = true;
                return null;
            }

            if (forwardScale < obstacleHandoff) return null;

            double worldAngle = Math.Atan2(goalX - robotX, goalY - robotY) * 180 / Math.PI;
            double relativeAngle = worldAngle - robotHeading;

            while (relativeAngle > 180) relativeAngle -= 360;
            while (relativeAngle < -180) relativeAngle += 360;

            return relativeAngle;
        }

        public static double GetDistanceToGoal(double robotX, double robotY)
        {
            double deltaX = goalX - robotX;
            double deltaY = goalY - robotY;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public static void Reset()
        {
            goalSet = false;
            goalReached = false;
            goalX = 0;
            goalY = 0;
        }
    }
}
