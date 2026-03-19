using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    internal class RiskCalculation
    {
        int sectors = 10;
        int distanceCells = 10;
        float sectorAngle = 0;
        float startAngle = 0;
        double rad = 0;

        public double[] EvaluateSectors()
        {
            double[] risks = new double[sectors];
            sectorAngle = 240f / sectors;

            for (int i = 0; i < sectors; i++)
            {
                startAngle = -120 + i * sectorAngle + sectorAngle / 2;
                risks[i] = EvaluateDirection(startAngle, distanceCells);
            }

            return risks;
        }

        public double EvaluateDirection(float angle, float distanceCells)
        {
            rad = angle * Math.PI / 180;

            double dx = Math.Cos(rad);
            double dy = Math.Sin(rad);

            double x = RobotMemory.gridCenter;
            double y = RobotMemory.gridCenter;

            double risk = 0;

            for (int i = 0; i < distanceCells; i++)
            {
                x += dx;
                y += dy;

                int gx = (int)x, gy = (int)y;

                if (x < 0 || x >= 200 || y < 0 || y >= 200) break;

                risk += RobotMemory.Grid[gx, gy];
            }

            return risk;
        }
    }
}
