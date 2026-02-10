using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SCIP_Library;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;

namespace LidarIndoorNavigation.ViewModels
{
    internal class MainWindowViewModel : ObservableObject
    {
        public static SerialPort? urg;
        int baudrate = 115200;
        string comPort = "COM6";

        const int GET_NUM = 1;
        const int start_step = 44;
        int end_step = 725;
        const double stepAngle = 0.3515625;

        List<long> distances = new();
        List<(double x, double y)> cartesianDistances = new();

        ObservableCollection<ObservablePoint> chartPoints = new();

        public IRelayCommand? StartCommand { get; }

        public MainWindowViewModel()
        {
            StartCommand = new RelayCommand(StartScan);

        }

        private void StartScan()
        {
            urg = new SerialPort(comPort, baudrate);

            urg.NewLine = "\n\n";
            urg.Open();

            // Initialize SCIP2
            urg.Write(SCIP_Writer.SCIP2());
            urg.ReadLine(); // ignore echo back

            // Start measurement (MD)
            urg.Write(SCIP_Writer.MD(start_step, end_step));
            string test = urg.ReadLine(); // ignore command echo

            if (urg != null)
            {
                for (int i = 0; i < GET_NUM; ++i)
                {
                    string receive_data = urg.ReadLine();                    
                    long time_stamp = 0;
                    SCIP_Reader.MD(receive_data, ref time_stamp, ref distances);
                }

                urg.Close();
                cartesianDistances = ConvertToCartesian(distances);

                chartPoints.Clear();
                foreach (var (x, y) in cartesianDistances)
                {
                    chartPoints.Add(new ObservablePoint(x, y));
                }
            }
        }

        public ISeries[] Series { get; set; } = [
                    new ScatterSeries<ObservablePoint>
                    {
                        Values = new ObservableCollection<ObservablePoint>()
                    }
                ];

        private List<(double x, double y)> ConvertToCartesian(List<long> distances)
        {
            List<(double x, double y)> points = new List<(double x, double y)>();
            for (int i = 0; i < distances.Count; ++i)
            {
                double angle = (start_step + i) * stepAngle;
                double x = distances[i] * Math.Cos(angle * Math.PI / 180);
                double y = distances[i] * Math.Sin(angle * Math.PI / 180);
                points.Add((x, y));
            }
            return points;
        }
    }
}