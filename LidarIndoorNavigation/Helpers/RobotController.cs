using LidarIndoorNavigation.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LidarIndoorNavigation.Helpers
{
    public class RobotController
    {
        //private Stream stream;
        //private Guid uuid = new Guid("8ce255c0-200a-11e0-ac64-0800200c9a66");
        //private Thread receiveThread;
        //private bool receiving;
        private SerialPort? selectedSerialPort1; //Seriová linka pre Motory
        private SerialPort? selectedSerialPort2; //Seriová linka pre Napájanie
        string SECUREMARK = "*";
        string completSequenceForOut = "5000";
        bool Electronic = false;
        bool Engine = false;

        public RobotController()
        {
            //StartListening();
            //labelReceivedData.TextChanged += LabelReceivedData_TextChanged;
        }

        //***************************************//
        //Nastavenie seriovej linky pre Napájanie//
        //***************************************//

        public void OpenSerialPort1(string portName)
        {
            if (portName != null || portName != string.Empty)
            {
                selectedSerialPort1 = new SerialPort(portName);
                selectedSerialPort1.BaudRate = 19200;
                selectedSerialPort1.DataBits = 8;
                selectedSerialPort1.Parity = Parity.None;
                selectedSerialPort1.StopBits = StopBits.One;
                try
                {
                    selectedSerialPort1.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening serial port 1: " + ex.Message);
                }
            }
        }

        //************************************//
        //Nastavenie seriovej linky pre Motory//
        //************************************//

        public void OpenSerialPort2(string portName)
        {
            if (portName != null || portName != string.Empty)
            {
                selectedSerialPort2 = new SerialPort(portName);
                selectedSerialPort2.BaudRate = 115200;
                selectedSerialPort2.DataBits = 8;
                selectedSerialPort2.Parity = Parity.None;
                selectedSerialPort2.StopBits = StopBits.One;
                try
                {
                    selectedSerialPort2.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening serial port 2: " + ex.Message);
                }
            }
        }

        //**********************************************//    //*************//
        //Tlačídlo MOSFET pre zapnutie MOSFET1 a MOSFET2//    //Serial Port 2//
        //**********************************************//    //*************//

        public void ElectronicButton()
        {
            if (selectedSerialPort1 != null && selectedSerialPort2 != null && selectedSerialPort1.IsOpen && selectedSerialPort2.IsOpen)
            {
                if (Electronic == false)
                {
                    selectedSerialPort1.Write("|F11\r");  //MOSFET1 ON
                    selectedSerialPort1.Write("|F21\r");  //MOSFET2 ON
                    Electronic = true;
                    System.Diagnostics.Debug.WriteLine("Electronic ON");
                }
                else
                {
                    selectedSerialPort1.Write("|F10\r");  //MOSFET1 OFF
                    selectedSerialPort1.Write("|F20\r");  //MOSFET2 OFF
                    Electronic = false;
                    System.Diagnostics.Debug.WriteLine("Electronic OFF");
                }
            }
            else
            {
            }
        }

        //**************************************//    //*************//
        //Tlačídlo Motory pre zapnutie  motorov//    //Serial Port 2//
        //************************************//    //*************//

        public void EngineButton()
        {
            if (selectedSerialPort1 != null && selectedSerialPort2 != null && selectedSerialPort1.IsOpen && selectedSerialPort2.IsOpen)
            {

                if (Electronic == true && Engine == true)
                {

                    selectedSerialPort2.Write("|MM0\r");  //MOTOR OFF
                    Engine = false;
                    System.Diagnostics.Debug.WriteLine("Engine OFF");
                }

                else if (Electronic == true && Engine == false)
                {
                    selectedSerialPort2.Write("|MM1\r");  //MOTOR ON
                    Engine = true;
                    System.Diagnostics.Debug.WriteLine("Engine ON");
                }
            }
        }


        //************************************//
        //Posielanie Stringu do seriovej linky// 
        //************************************//

        internal void Movement(MovementCommands command)
        {
            System.Diagnostics.Debug.WriteLine(command);


            if (selectedSerialPort2 != null && selectedSerialPort2.IsOpen)
            {
                try
                {
                    if (command == MovementCommands.Forward)   /*FORWARD*/
                    {

                        string controlCommand = ("A" + "F" + completSequenceForOut + "F" + completSequenceForOut + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + completSequenceForOut + "F" + completSequenceForOut + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);

                        System.Diagnostics.Debug.WriteLine(controlCommand);
                        System.Diagnostics.Debug.WriteLine(controlCommand2);
                    }

                    //if (labelReceivedData.Text == "Backwards")   /*BACKWARDS*/
                    /*{

                        string controlCommand = ("A" + "B" + completSequenceForOut + "B" + completSequenceForOut + SECUREMARK);
                        selectedSerialPort1.Write(controlCommand);

                        string controlCommand2 = ("C" + "B" + completSequenceForOut + "B" + completSequenceForOut + SECUREMARK);
                        selectedSerialPort1.Write(controlCommand2);

                    }*/

                    if (command == MovementCommands.TurnLeft)   /*LEFT*/
                    {

                        string controlCommand = ("A" + "B" + completSequenceForOut + "F" + completSequenceForOut + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "B" + completSequenceForOut + "F" + completSequenceForOut + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);

                        System.Diagnostics.Debug.WriteLine(controlCommand);
                        System.Diagnostics.Debug.WriteLine(controlCommand2);
                    }

                    if (command == MovementCommands.TurnRight)   /*RIGHT*/
                    {

                        string controlCommand = ("A" + "F" + completSequenceForOut + "B" + completSequenceForOut + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + completSequenceForOut + "B" + completSequenceForOut + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);

                        System.Diagnostics.Debug.WriteLine(controlCommand);
                        System.Diagnostics.Debug.WriteLine(controlCommand2);
                    }

                    if (command == MovementCommands.Stop)   /*STOP*/
                    {

                        string controlCommand = ("A" + "F" + "0" + "F" + "0" + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + "0" + "F" + "0" + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);

                        System.Diagnostics.Debug.WriteLine(controlCommand);
                        System.Diagnostics.Debug.WriteLine(controlCommand2);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error sending string to robot: " + ex.Message);
                }
            }
        }

        //******************//
        //Zapnutie počúvania//
        //******************//

        /*private void StartListening()
        {
            try
            {
                bluetoothListener = new BluetoothListener(uuid);
                bluetoothListener.Start();
                bluetoothListener.BeginAcceptBluetoothClient(AcceptCallback, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri počúvaní: " + ex.Message);

            }
        }*/

        //********//
        //Callback//
        //********//

        /*private void AcceptCallback(IAsyncResult result)
        {
            try
            {
                BluetoothClient client = bluetoothListener.EndAcceptBluetoothClient(result);
                stream = client.GetStream();
                tbInfo.Invoke(new Action(() =>
                {
                    tbInfo.AppendText("Zariadenie je prepojené." + Environment.NewLine);
                }));
                StartReceiving();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri akceptovaní klienta: " + ex.Message);
                WaitForConnection();
            }
        }*/

        //*******************//
        //Zapnutie príjamania//
        //*******************//

        /*private void StartReceiving()
        {
            receiving = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
        }*/

        //*************************//
        //Prímanie Stringu do Label//
        //*************************//

        /*private void ReceiveData()
        {
            while (receiving)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        tbInfo.Invoke(new Action(() =>
                        {
                            tbInfo.AppendText("Pripojenie bolo prerušené." + Environment.NewLine);
                        }));

                        WaitForConnection();
                        break;
                    }
                    string receivedData = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (labelReceivedData.InvokeRequired)
                    {
                        if (!labelReceivedData.IsDisposed && labelReceivedData.IsHandleCreated)
                        {
                            labelReceivedData.Invoke(new Action(() =>
                            {
                                labelReceivedData.Text = receivedData;
                            }));
                        }
                    }
                    else
                    {
                        if (!labelReceivedData.IsDisposed)
                        {
                            labelReceivedData.Text = receivedData;
                        }
                    }
                }
                catch (IOException)
                {

                }


            }

        }*/

        //*******************************//
        //Čakanie na obnovenie pripojenia//
        //*******************************//

        /*private void WaitForConnection()
        {

            try
            {
                bluetoothListener.BeginAcceptBluetoothClient(AcceptCallback, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri čakaní na pripojenie: " + ex.Message);
            }
        }*/

        //*********//
        //Odpojenie//
        //*********//

        /*private void Disconnect()
        {
            try
            {
                receiving = false;
                if (stream != null)
                {
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri odpojovaní: " + ex.Message);
            }
        }*/

        //*******************//
        //Panel a.k.a pozadie//
        //*******************//

        //private void panel1_Paint(object sender, PaintEventArgs e)
        //{
            /*panel1.BackgroundImage = Properties.Resources.spu_logo;
            panel1.BackgroundImageLayout = ImageLayout.Stretch;*/
            //panel1.BackColor = Color.White;
            //Graphics g = e.Graphics;
            //Graphics h = e.Graphics;
            //Pen blkpen = new Pen(Color.Black, 5);
            //g.DrawRectangle(blkpen, new Rectangle(1, 1, 695, 365));
            /*h.DrawRectangle(blkpen, new Rectangle(40,40,100,100));*/

        //}

        //***********************************//
        //Tlačídlo Exit pre vypnutie programu//
        //***********************************//

        internal void ClosePorts()
        {
            if (selectedSerialPort1 != null && selectedSerialPort1.IsOpen && selectedSerialPort2 != null && selectedSerialPort2.IsOpen)
            {
                selectedSerialPort1.Close();
                selectedSerialPort2.Close();

                MessageBox.Show("Robot serial ports were closed successfully.");
            }
        }
    }
}
