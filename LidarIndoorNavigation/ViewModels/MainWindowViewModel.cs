using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LidarIndoorNavigation.Helpers;
using LidarIndoorNavigation.Models;
using LidarIndoorNavigation.Views;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using SCIP_Library;
using System.Collections.ObjectModel;
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
        private bool electronicsRunning;
        [ObservableProperty]
        private bool engineRunning;
        [ObservableProperty]
        private bool scannerOpen;
        [ObservableProperty]
        private bool electronicsOpen;
        [ObservableProperty]
        private bool engineOpen;

        [ObservableProperty]
        private BitmapSource? gridImage;

        [ObservableProperty]
        private ObservableCollection<SectorRiskItem> sectorRisks = new();

        [ObservableProperty]
        private double frontalRisk;

        [ObservableProperty]
        private double angle;

        [ObservableProperty]
        private double magnitude;

        [ObservableProperty]
        private string command = string.Empty;

        [ObservableProperty]
        private int distanceSliderValue = 15;
        [ObservableProperty]
        private int frontRiskSliderValue = 1;
        [ObservableProperty]
        private int sectorCountSliderValue = 20;

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public IAsyncRelayCommand? StartCommand { get; }
        public IRelayCommand? StopCommand { get; }
        public IRelayCommand? OpenPortsCommand { get; }
        public IAsyncRelayCommand? ElectronicCommand { get; }
        public IRelayCommand? EngineCommand { get; }
        public IRelayCommand? ForwardCommand { get; }
        public IRelayCommand? LeftCommand { get; }
        public IRelayCommand? RightCommand { get; }
        public IRelayCommand? BackwardCommand { get; }
        public IRelayCommand? StopRobotCommand { get; }
        public IRelayCommand? OpenWindowsCommand { get; }
        public IRelayCommand? RefreshPortsCommand { get; }

        public MainWindowViewModel()
        {
            StartCommand = new AsyncRelayCommand(StartScan);
            StopCommand = new RelayCommand(StopScan);
            OpenPortsCommand = new RelayCommand(OpenPorts);
            ElectronicCommand = new AsyncRelayCommand(Electronic);
            EngineCommand = new RelayCommand(Engine);
            ForwardCommand = new RelayCommand(Forward);
            LeftCommand = new RelayCommand(Left);
            RightCommand = new RelayCommand(Right);
            BackwardCommand = new RelayCommand(Backward);
            StopRobotCommand = new RelayCommand(Stop);
            RefreshPortsCommand = new RelayCommand(RefreshPorts);
            OpenWindowsCommand = new RelayCommand(OpenWindows);

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
                    MaxLimit = 3000,
                    MinStep = 500,
                    ForceStepToMin = true
                }
            ];

            YAxes =
            [
                new Axis
                {
                    MinLimit = -3000,
                    MaxLimit = 3000,
                    MinStep = 500,
                    ForceStepToMin = true
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
                        ScannerOpen = urg.IsOpen;
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
                        ScannerOpen = urg.IsOpen;
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

                        cartesianDistances.Clear();
                        cartesianDistances.AddRange(cartesianConverter.ConvertToCartesian(distances));

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
                            MessageBox.Show($"Error updating chart: {ex.Message}");
                        }

                        robotMemory.EnqueueScan(cartesianDistances.ToList());

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            GridImage = robotMemory.RenderGrid(DistanceSliderValue, SectorCountSliderValue);
                        });

                        (double moveAngle, double[] risks, double frontRisk, double currMagnitude) = reactiveNavigation.DecideMovement(DistanceSliderValue, FrontRiskSliderValue, SectorCountSliderValue);

                        var (command, forwardScale) = reactiveNavigation.GetCommand(moveAngle);

                        RobotController.Movement(command);

                        UpdateData(risks, frontRisk, moveAngle, currMagnitude, command.ToString());
                    }
                    RobotController.Movement(MovementCommands.Stop);
                    urg.Close();
                    ScannerOpen = urg.IsOpen;
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
                    ElectronicsOpen = RobotController.OpenSerialPort1(SelectedPort2);
                    EngineOpen = RobotController.OpenSerialPort2(SelectedPort3);
                    arePortsOpen = true;
                }
                else
                {
                    var portsOpenBool = RobotController.ClosePorts();
                    ElectronicsOpen = portsOpenBool.port1;
                    EngineOpen = portsOpenBool.port2;
                    arePortsOpen = false;
                }
            }
        }

        private async Task Electronic()
        {
            ElectronicsRunning = RobotController.ElectronicButton();
            await Task.Delay(1000);
            RefreshPorts();
        }

        private void Engine()
        {
            EngineRunning = RobotController.EngineButton();
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

        private void RefreshPorts()
        {
            var port1 = SelectedPort1;
            var port2 = SelectedPort2;
            var port3 = SelectedPort3;

            ports.Clear();
            ComPorts.Clear();
            ports = SerialPort.GetPortNames().ToList();
            foreach (string port in ports)
            {
                ComPorts.Add(port);
            }

            SelectedPort1 = port1;
            SelectedPort2 = port2;
            SelectedPort3 = port3;
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

        public void UpdateData(double[] sectorRisks, double frontalRisk, double angle, double magnitude, string command)
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
                Magnitude = magnitude;
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