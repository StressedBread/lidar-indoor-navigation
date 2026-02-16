using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using SCIP_Library;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using LidarIndoorNavigation.Helpers;

namespace LidarIndoorNavigation.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
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

        PolarToCartesianConverter cartesianConverter = new();
        ReactiveNavigation reactiveNavigation = new();
        RobotController robotController = new();

        private CancellationTokenSource cts = new();

        ObservableCollection<ObservablePoint> chartPoints = new();
        public ObservableCollection<string> ComPorts { get; } = new();

        [ObservableProperty]
        private string selectedPort = "";

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public IRelayCommand? StartCommand { get; }
        public IRelayCommand? StopCommand { get; }

        public MainWindowViewModel()
        {
            StartCommand = new RelayCommand(StartScan);
            StopCommand = new RelayCommand(StopScan);

            LoadSerialPorts();

            Series =
            [
                new ScatterSeries<ObservablePoint>
                {
                    Values = chartPoints,
                    GeometrySize = 10
                }
            ];

            XAxes =
            [
                new Axis
                {
                    MinLimit = -3000,
                    MaxLimit = 3000
                }
            ];

            YAxes =
            [
                new Axis
                {
                    MinLimit = -3000,
                    MaxLimit = 3000
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
                                break; 
                            }
                            catch (IOException)
                            {
                                break; 
                            }
                            long time_stamp = 0;
                            SCIP_Reader.MD(receive_data, ref time_stamp, ref distances);
                            
                            DistancePointsStaticList.Clear();
                            DistancePointsStaticList.Distances.AddRange(distances);

                            cartesianDistances = cartesianConverter.ConvertToCartesian(DistancePointsStaticList.Distances);

                            try
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    chartPoints.Clear();
                                    foreach (var (x, y) in cartesianDistances)
                                    {
                                        chartPoints.Add(new ObservablePoint(x, y));
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error updating chart: {ex.Message}");
                            }
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
            if (urg != null)
            {
                urg.Write(SCIP_Writer.QT()); // stop measurement mode
                urg.ReadLine(); // ignore echo back
                urg.Close();
            }
        }

        private void LoadSerialPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                ComPorts.Add(port);
            }
        }

        [RelayCommand]
        private void OpenPort()
        {
            if (!string.IsNullOrEmpty(SelectedPort))
            {
                robotController.OpenSerialPort1(SelectedPort);
                robotController.OpenSerialPort2(SelectedPort);
            }
        }

        [RelayCommand]
        private void ElectronicCommand()
        {
            robotController.ElectronicButton();
        }

        [RelayCommand]
        private void EngineCommand()
        {
            robotController.EngineButton();
        }
    }
}