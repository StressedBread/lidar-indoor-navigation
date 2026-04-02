using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LidarIndoorNavigation.Helpers;
using LidarIndoorNavigation.Models;
using LidarIndoorNavigation.Views;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using SCIP_Library;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LidarIndoorNavigation.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        public static SerialPort? urg;
        int baudrate = 115200;

        bool arePortsOpen = false;
        List<string> ports = new();

        const int start_step = 44;
        int end_step = 725;

        List<long> distances = new();
        List<(double x, double y)> cartesianDistances = new();

        PolarToCartesianConverter cartesianConverter = new();
        ReactiveNavigation reactiveNavigation = new();
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

        [ObservableProperty]
        private BitmapSource? gridImage;

        [ObservableProperty]
        private ObservableCollection<SectorRiskItem> sectorRisks = new();

        [ObservableProperty]
        private double frontalRisk;

        [ObservableProperty]
        private double angle;

        [ObservableProperty]
        private string command = string.Empty;

        [ObservableProperty]
        private int distanceSliderValue;

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public IAsyncRelayCommand? StartCommand { get; }
        public IRelayCommand? StopCommand { get; }
        public IRelayCommand? OpenPortsCommand { get; }
        public IRelayCommand? ElectronicCommand { get; }
        public IRelayCommand? EngineCommand { get; }
        public IRelayCommand? ForwardCommand { get; }
        public IRelayCommand? LeftCommand { get; }
        public IRelayCommand? RightCommand { get; }
        public IRelayCommand? BackwardCommand { get; }
        public IRelayCommand? StopRobotCommand { get; }
        public IRelayCommand? OpenWindowsCommand { get; }
        public IRelayCommand? TestDataMemoryCommand { get; }
        public IRelayCommand? RefreshPortsCommand { get; }

        public MainWindowViewModel()
        {
            StartCommand = new AsyncRelayCommand(StartScan);
            StopCommand = new RelayCommand(StopScan);
            OpenPortsCommand = new RelayCommand(OpenPorts);
            ElectronicCommand = new RelayCommand(Electronic);
            EngineCommand = new RelayCommand(Engine);
            ForwardCommand = new RelayCommand(Forward);
            LeftCommand = new RelayCommand(Left);
            RightCommand = new RelayCommand(Right);
            BackwardCommand = new RelayCommand(Backward);
            StopRobotCommand = new RelayCommand(Stop);
            TestDataMemoryCommand = new RelayCommand(TestDataMemory);
            RefreshPortsCommand = new RelayCommand(RefreshPorts);
            OpenWindowsCommand = new RelayCommand(OpenWindows);

            LoadSerialPorts();
            WaypointNavigator.SetGoal(-1000, 1500);

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
            robotMemory.StartBackgroundProcessing(cts.Token);

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

                        /*(int R, int Mid, int L) minDistances = reactiveNavigation.CalculateMinDistanceLessSectors();
                        System.Diagnostics.Debug.WriteLine(minDistances);
                        var decision = reactiveNavigation.DecisionLogicLessSectors(minDistances);*/

                        //RobotController.Movement(decision);

                        robotMemory.EnqueueScan(DistancePointsStaticList.CartesianDistances.ToList());

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            GridImage = robotMemory.RenderGrid(DistanceSliderValue);
                        });

                        (double moveAngle, double[] risks, double frontRisk) = reactiveNavigation.DecideMovement(DistancePointsStaticList.CartesianDistances, DistanceSliderValue);

                        var (command, forwardScale) = reactiveNavigation.GetCommand(moveAngle);

                        RobotController.Movement(command);

                        UpdateData(risks, frontRisk, moveAngle, command.ToString());
                        //var (leftSpeed, rightSpeed) = RobotController.AngleToWheelSpeeds(reactiveNavigation.moveAngle, forwardScale, command == MovementCommands.Stop);

                        System.Diagnostics.Debug.WriteLine("\n********");
                        System.Diagnostics.Debug.WriteLine(moveAngle);
                        System.Diagnostics.Debug.WriteLine(command);
                        System.Diagnostics.Debug.WriteLine("********\n");
                        //System.Diagnostics.Debug.WriteLine(leftSpeed + " || " + rightSpeed);

                        //RobotController.SetMovement(leftSpeed, rightSpeed);
                    }
                    RobotController.Movement(MovementCommands.Stop);
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
            ports = SerialPort.GetPortNames().ToList();
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
                    RobotController.OpenSerialPort1(SelectedPort2);
                    RobotController.OpenSerialPort2(SelectedPort3);
                    arePortsOpen = true;
                }
                else
                {
                    RobotController.ClosePorts();
                    arePortsOpen = false;
                }
            }
        }

        private void Electronic()
        {
            RobotController.ElectronicButton();
        }

        private void Engine()
        {
            RobotController.EngineButton();
        }


        private void Forward()
        {
            RobotController.Movement(MovementCommands.Forward);
        }

        private void Left()
        {
            RobotController.Movement(MovementCommands.TurnLeft);
        }

        private void Right()
        {
            RobotController.Movement(MovementCommands.TurnRight);
        }

        private void Backward()
        {
            RobotController.Movement(MovementCommands.Backward);
        }

        private void Stop()
        {
            RobotController.Movement(MovementCommands.Stop);
        }

        private void TestDataMemory()
        {
            //robotMemory.UpdateMemory();
        }

        private void RefreshPorts()
        {
            ports.Clear();
            ComPorts.Clear();
            ports = SerialPort.GetPortNames().ToList();
            foreach (string port in ports)
            {
                ComPorts.Add(port);
            }
        }

        private void OpenGridImageViewer()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var viewer = new GridImageView
                {
                    DataContext = this
                };
                viewer.HorizontalAlignment = HorizontalAlignment.Center;
                viewer.Show();
            });
        }

        private void OpenDataWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dataWindow = new DataViewerView
                {
                    DataContext = this
                };
                dataWindow.Show();
            });
        }

        public void UpdateData(double[] sectorRisks, double frontalRisk, double angle, string command)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SectorRisks.Clear();
                for (int i = 0; i < sectorRisks.Length; i++)
                {
                    SectorRisks.Add(new SectorRiskItem(i, sectorRisks[i]));
                }

                FrontalRisk = frontalRisk;
                Angle = angle;
                Command = command;
            });
        }

        public void OpenWindows()
        {
            OpenDataWindow();
            OpenGridImageViewer();
        }
    }
}