using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    internal class WaypointNavigator
    {
        private double arrivalRadius = 20;
        private double obstacleHandoff = 0.5;

        private double goalX = 0;
        private double goalY = 0;
        private bool goalSet = false;

        public bool goalReached = false;

        public void SetGoal(double x, double y)
        {
            goalX = x;
            goalY = y;
            goalSet = true;
            goalReached = false;
        }

        public double? GetSteeringAgle(double robotX, double robotY, double robotHeading, double forwardScale)
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

        public double GetDistanceToGoal(double robotX, double robotY)
        {
            double deltaX = goalX - robotX;
            double deltaY = goalY - robotY;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public void Reset()
        {
            goalSet = false;
            goalReached = false;
            goalX = 0;
            goalY = 0;
        }
    }
}
