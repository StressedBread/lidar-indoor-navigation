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

        public float[] EvaluateSectors()
        {
            float[] risks = new float[sectors];
            sectorAngle = 240f / sectors;

            for (int i = 0; i < sectors; i++)
            {
                startAngle = -120 + i * sectorAngle + sectorAngle / 2;
                risks[i] = EvaluateDirection(startAngle, distanceCells);
            }

            return risks;
        }

        public float EvaluateDirection(float angle, float distanceCells)
        {
            rad = angle * Math.PI / 180;

            int dx = (int)Math.Floor(Math.Cos(rad));
            int dy = (int)Math.Floor(Math.Sin(rad));

            int x = RobotMemory.gridCenter;
            int y = RobotMemory.gridCenter;

            float risk = 0;

            for (int i = 0; i < distanceCells; i++)
            {
                x += dx;
                x += dy;

                if (x < 0 || x >= 200 || y < 0 || y >= 200) break;

                risk += RobotMemory.Grid[x, y];
            }

            return risk;
        }
    }
}
