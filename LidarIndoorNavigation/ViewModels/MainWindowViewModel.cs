using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using SCIP_Library;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;

namespace LidarIndoorNavigation.ViewModels
{
    internal class MainWindowViewModel : ObservableObject
    {
        public static SerialPort? urg;
        int baudrate = 115200;
        string comPort = "COM7";

        const int GET_NUM = 1;
        const int start_step = 44;
        int end_step = 725;
        const double stepAngle = 0.3515625;

        List<long> distances = new();
        List<(double x, double y)> cartesianDistances = new();

        private CancellationTokenSource cts = new();

        ObservableCollection<ObservablePoint> chartPoints = new();
        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public IRelayCommand? StartCommand { get; }
        public IRelayCommand? StopCommand { get; }

        public MainWindowViewModel()
        {
            StartCommand = new RelayCommand(StartScan);
            StopCommand = new RelayCommand(StopScan);

            Series =
            [
                new ScatterSeries<ObservablePoint>
                {
                    Values = chartPoints
                }
            ];

            XAxes =
            [
                new Axis
                {
                    MinLimit = -5600,
                    MaxLimit = 5600
                }
            ];

            YAxes =
            [
                new Axis
                {
                    MinLimit = -5600,
                    MaxLimit = 5600
                }
            ];
        }



        private void StartScan()
        {
            cts = new CancellationTokenSource();
            var token = cts.Token;

            try { 
                urg = new SerialPort(comPort, baudrate);

                urg.NewLine = "\n\n";
                urg.Open();

                // Initialize SCIP2
                urg.Write(SCIP_Writer.SCIP2());
                urg.ReadLine(); // ignore echo back

                // Start measurement (MD)
                urg.Write(SCIP_Writer.MD(start_step, end_step));
                urg.ReadLine(); // ignore command echo

                if (urg != null)
                {
                    Task.Run(() =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            string receive_data = string.Empty;
                            try
                            {
                                receive_data = urg.ReadLine(); // blocking read
                            }
                            catch (OperationCanceledException)
                            {
                                break; // user pressed stop
                            }
                            catch (IOException)
                            {
                                break; // serial port closed
                            }
                            long time_stamp = 0;
                            SCIP_Reader.MD(receive_data, ref time_stamp, ref distances);
                            
                            cartesianDistances = ConvertToCartesian(distances);

                            App.Current.Dispatcher.Invoke(() =>
                            {
                                chartPoints.Clear();
                                foreach (var (x, y) in cartesianDistances)
                                {
                                    chartPoints.Add(new ObservablePoint(x, y));
                                }
                            });
                        }
                        urg.Close();
                    }, token);
                }
                
            }           
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void StopScan()
        {
            cts.Cancel();
            if (urg != null && urg.IsOpen)
            {
                urg.Close();
            }
        }

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