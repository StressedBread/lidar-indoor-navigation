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
        int distanceCells = 15;
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

        public double EvaluateDirection(float angle, float distanceCells)
        {
            // cast N rays spread across the 12° sector width
            int rayCount = 5;
            double sectorHalfWidth = 6.0; // half of 12°
            double totalRisk = 0;

            for (int r = 0; r < rayCount; r++)
            {
                double offset = -sectorHalfWidth + r * (sectorHalfWidth * 2 / (rayCount - 1));
                double rayAngle = angle + offset;
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
