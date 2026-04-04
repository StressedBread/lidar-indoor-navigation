using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarIndoorNavigation.Helpers
{
    internal class RiskCalculation
    {
        int sectors = 20;
        //int distanceCells = 15;
        double sectorAngle = 0;
        double startAngle = 0;
        double endAngle = 0;
        double rad = 0;

        public double[] EvaluateSectors(int distanceCells)
        {
            double[] risks = new double[sectors];
            sectorAngle = 240 / sectors;

            for (int i = 0; i < sectors; i++)
            {
                startAngle = -120 + i * sectorAngle;
                endAngle = startAngle + sectorAngle;
                risks[i] = EvaluateDirection(startAngle, endAngle, distanceCells);
            }

            return risks;
        }

        /*public double EvaluateDirection(float angle, float distanceCells)
        {
            rad = angle * Math.PI / 180;

            double dx = Math.Sin(rad);
            double dy = -Math.Cos(rad);

            double x = RobotMemory.gridCenter;
            double y = RobotMemory.gridCenter;

            double risk = 0;

            for (int i = 0; i < distanceCells; i++)
            {
                x += dx;
                y += dy;

                int gx = (int)x, gy = (int)y;

                if (x < 0 || x >= 201 || y < 0 || y >= 201) break;

                risk += RobotMemory.Grid[gx, gy];
            }

            return risk;
        }*/

        public double EvaluateDirection(double startSectorAngle, double endSectorAngle, double distanceCells)
        {
            int rayCount = 5;
            double totalRisk = 0;

            for (int r = 0; r < rayCount; r++)
            {
                double rayAngle = startSectorAngle + r * ((endSectorAngle - startSectorAngle) / (rayCount - 1));
                rad = rayAngle * Math.PI / 180;

                double dx = Math.Sin(rad);
                double dy = -Math.Cos(rad);
                double x = RobotMemory.gridCenter;
                double y = RobotMemory.gridCenter;

                for (int i = 0; i < distanceCells; i++)
                {
                    x += dx; y += dy;
                    int gx = (int)x, gy = (int)y;
                    if (x < 0 || x >= 201 || y < 0 || y >= 201) break;
                    totalRisk += RobotMemory.Grid[gx, gy];
                }
            }

            return totalRisk;
        }
    }
}
