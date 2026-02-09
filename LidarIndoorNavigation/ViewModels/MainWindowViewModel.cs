using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SCIP_Library;


namespace LidarIndoorNavigation.ViewModels
{
    internal class MainWindowViewModel : ObservableObject
    {
        public static SerialPort? urg;
        int baudrate = 115200;
        string comPort = "COM5";

        const int GET_NUM = 10;
        const int start_step = 44;
        int end_step = 725;
        const double stepAngle = 0.3515625;

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
                long lastTimeStamp = 0;
                for (int i = 0; i <= GET_NUM; ++i)
                {
                    string receive_data = urg.ReadLine();
                    List<long> distances = new List<long>();
                    long time_stamp = 0;
                    SCIP_Reader.MD(receive_data, ref time_stamp, ref distances);

                    System.Diagnostics.Debug.WriteLine($"Delta between scans: {time_stamp - lastTimeStamp} ms");
                    lastTimeStamp = time_stamp;
                }
            }
        }
    }
}
