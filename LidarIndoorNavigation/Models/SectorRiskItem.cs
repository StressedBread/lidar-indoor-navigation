namespace LidarIndoorNavigation.Models
{
    public class SectorRiskItem
    {
        public int SectorNumber { get; set; }
        public double Risk { get; set; }

        public SectorRiskItem(int sectorNumber, double risk)
        {
            SectorNumber = sectorNumber;
            Risk = risk;
        }
    }
}
