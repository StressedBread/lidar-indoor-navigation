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
using System.Windows;
using LidarIndoorNavigation.Models;

namespace LidarIndoorNavigation.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        public static SerialPort? urg;
        int baudrate = 115200;

        bool arePortsOpen = false;

        const int start_step = 44;
        int end_step = 725;

        List<long> distances = new();
        List<(double x, double y)> cartesianDistances = new();

        PolarToCartesianConverter cartesianConverter = new();
        ReactiveNavigation reactiveNavigation = new();
        RobotController robotController = new();
        RobotMemory robotMemory = new RobotMemory();

        private CancellationTokenSource cts = new();

        ObservableCollection<ObservablePoint> chartPoints = new();
        public ObservableCollection<string> ComPorts { get; } = new();

        [ObservableProperty]
        private string? selectedPort1 = "";
        [ObservableProperty]
        private string? selectedPort2 = "";
        [ObservableProperty]
        private string? selectedPort3 = "";

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public IAsyncRelayCommand? StartCommand { get; }
        public IRelayCommand? StopCommand { get; }
        public IRelayCommand? OpenPortsCommand { get; }
        public IRelayCommand? ElectronicCommand { get; }
        public IRelayCommand? EngineCommand { get; }
        public IAsyncRelayCommand? CheckPortsCommand { get; }

        public IAsyncRelayCommand? ForwardCommand { get; }
        public IAsyncRelayCommand? LeftCommand { get; }
        public IAsyncRelayCommand? RightCommand { get; }
        public IAsyncRelayCommand? StopRobotCommand { get; }

        public IRelayCommand? TestDataMemoryCommand { get; }

        public MainWindowViewModel()
        {
            StartCommand = new AsyncRelayCommand(StartScan);
            StopCommand = new RelayCommand(StopScan);
            OpenPortsCommand = new RelayCommand(OpenPorts);
            ElectronicCommand = new RelayCommand(Electronic);
            EngineCommand = new RelayCommand(Engine);
            CheckPortsCommand = new AsyncRelayCommand(CheckPorts);
            ForwardCommand = new AsyncRelayCommand(Forward);
            LeftCommand = new AsyncRelayCommand(Left);
            RightCommand = new AsyncRelayCommand(Right);
            StopRobotCommand = new AsyncRelayCommand(Stop);
            TestDataMemoryCommand = new RelayCommand(TestDataMemory);

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

        private async Task StartScan()
        {
            cts = new CancellationTokenSource();
            var token = cts.Token;

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        urg = new SerialPort(SelectedPort1, baudrate)
                        {
                            NewLine = "\n\n",
                            ReadTimeout = 3000,
                            WriteTimeout = 3000
                        };
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error initializing serial port: {ex.Message}");
                        return;
                    }

                    try
                    {
                        urg.Open();

                        // Initialize SCIP2
                        urg.Write(SCIP_Writer.SCIP2());
                        urg.ReadLine(); // ignore echo back

                        // Start measurement (MD)
                        urg.Write(SCIP_Writer.MD(start_step, end_step));
                        urg.ReadLine(); // ignore command echo
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening serial port: {ex.Message}");
                        urg.Close();
                        return;
                    }

                    while (!token.IsCancellationRequested)
                    {
                        string receive_data = string.Empty;
                        try
                        {
                            receive_data = urg.ReadLine(); // blocking read
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        long time_stamp = 0;
                        SCIP_Reader.MD(receive_data, ref time_stamp, ref distances);

                        DistancePointsStaticList.Clear();
                        DistancePointsStaticList.Distances.AddRange(distances);

                        cartesianDistances = cartesianConverter.ConvertToCartesian(DistancePointsStaticList.Distances);

                        DistancePointsStaticList.CartesianClear();
                        DistancePointsStaticList.CartesianDistances.AddRange(cartesianDistances);

                        try
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                chartPoints.Clear();
                                foreach (var (x, y) in DistancePointsStaticList.CartesianDistances)
                                {
                                    chartPoints.Add(new ObservablePoint(x, y));
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error updating chart: {ex.Message}");
                        }

                        (int R, int Mid, int L) minDistances = reactiveNavigation.CalculateMinDistanceLessSectors();
                        System.Diagnostics.Debug.WriteLine(minDistances);
                        var decision = reactiveNavigation.DecisionLogicLessSectors(minDistances);

                        robotController.Movement(decision);
                    }
                    robotController.Movement(MovementCommands.Stop);
                    urg.Close();
                }, token);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void StopScan()
        {
            cts.Cancel();
            if (urg != null)
            {
                urg.Write(SCIP_Writer.QT()); // stop measurement mode
                chartPoints.Clear();
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

        private void OpenPorts()
        {
            if (!string.IsNullOrEmpty(SelectedPort2) && !string.IsNullOrEmpty(SelectedPort3))
            {
                if (!arePortsOpen)
                {
                    robotController.OpenSerialPort1(SelectedPort2);
                    robotController.OpenSerialPort2(SelectedPort3);
                    arePortsOpen = true;
                }
                else
                {
                    robotController.ClosePorts();
                    arePortsOpen = false;
                }
            }
        }

        private void Electronic()
        {
            robotController.ElectronicButton();
        }

        private void Engine()
        {
            robotController.EngineButton();
        }

        private async Task CheckPorts()
        {

        }

        private async Task Forward()
        {
            robotController.Movement(MovementCommands.Forward);
        }

        private async Task Left()
        {
            robotController.Movement(MovementCommands.TurnLeft);
        }

        private async Task Right()
        {
            robotController.Movement(MovementCommands.TurnRight);
        }

        private async Task Stop()
        {
            robotController.Movement(MovementCommands.Stop);
        }

        private void TestDataMemory()
        {
            robotMemory.UpdateMemory();
        }
    }
}