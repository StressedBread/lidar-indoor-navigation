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
    public static class RobotController
    {
        //Odber = 1.2A, pri zmene smeru 1.8A

        private static SerialPort? selectedSerialPort1; //Seriová linka pre Motory
        private static SerialPort? selectedSerialPort2; //Seriová linka pre Napájanie
        private static string SECUREMARK = "*";
        private static string maxSpeedString = "5000";
        private static int maxSpeed = 5000;
        private static int minSpeed = 2500;
        private static bool Electronic = false;
        private static bool Engine = false;

        private static double currentLeft = 0;
        private static double currentRight = 0;
        private static double rampRate = 0.5;

        //***************************************//
        //Nastavenie seriovej linky pre Napájanie//
        //***************************************//

        public static void OpenSerialPort1(string portName)
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
                    MessageBox.Show("Port 1 open");
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

        public static void OpenSerialPort2(string portName)
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
                    MessageBox.Show("Port 2 open");
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

        public static void ElectronicButton()
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

        public static void EngineButton()
        {
            if (selectedSerialPort1 != null && selectedSerialPort2 != null && selectedSerialPort1.IsOpen && selectedSerialPort2.IsOpen)
            {

                if (Electronic == true && Engine == true)
                {

                    selectedSerialPort1.Write("|MM0\r");  //MOTOR OFF
                    Engine = false;
                    System.Diagnostics.Debug.WriteLine("Engine OFF");
                }

                else if (Electronic == true && Engine == false)
                {
                    selectedSerialPort1.Write("|MM1\r");  //MOTOR ON
                    Engine = true;
                    System.Diagnostics.Debug.WriteLine("Engine ON");
                }
            }
        }


        //************************************//
        //Posielanie Stringu do seriovej linky// 
        //************************************//

        internal static void Movement(MovementCommands command)
        {
            System.Diagnostics.Debug.WriteLine(command);


            if (selectedSerialPort2 != null && selectedSerialPort2.IsOpen)
            {
                try
                {
                    if (command == MovementCommands.Forward)   /*FORWARD*/
                    {

                        string controlCommand = ("A" + "F" + maxSpeedString + "F" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + maxSpeedString + "F" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);

                        System.Diagnostics.Debug.WriteLine(controlCommand);
                        System.Diagnostics.Debug.WriteLine(controlCommand2);
                    }

                    if (command == MovementCommands.Backward)   /*BACKWARDS*/
                    {

                        string controlCommand = ("A" + "B" + maxSpeedString + "B" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "B" + maxSpeedString + "B" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);

                        System.Diagnostics.Debug.WriteLine(controlCommand);
                        System.Diagnostics.Debug.WriteLine(controlCommand2);

                    }

                    if (command == MovementCommands.TurnLeft)   /*LEFT*/
                    {

                        string controlCommand = ("A" + "B" + maxSpeedString + "F" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "B" + maxSpeedString + "F" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);

                        System.Diagnostics.Debug.WriteLine(controlCommand);
                        System.Diagnostics.Debug.WriteLine(controlCommand2);
                    }

                    if (command == MovementCommands.TurnRight)   /*RIGHT*/
                    {

                        string controlCommand = ("A" + "F" + maxSpeedString + "B" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + maxSpeedString + "B" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);

                        System.Diagnostics.Debug.WriteLine(controlCommand);
                        System.Diagnostics.Debug.WriteLine(controlCommand2);
                    }

                    if (command == MovementCommands.Stop)   /*STOP*/
                    {

                        string controlCommand = ("A" + "F" + "0000" + "F" + "0000" + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + "0000" + "F" + "0000" + SECUREMARK);
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

        internal static void SetMovement(double leftTarget, double rightTarget)
        {
            if (leftTarget == 0 && rightTarget == 0)
            {
                currentLeft = 0;
                currentRight = 0;
            }
            else
            {
                currentLeft = Lerp(currentLeft, leftTarget, rampRate);
                currentRight = Lerp(currentRight, rightTarget, rampRate);
            }

            SendWheels(currentLeft, currentRight);
        }

        internal static void SendWheels(double left, double right)
        {
            if (selectedSerialPort2 != null && selectedSerialPort2.IsOpen)
            {
                selectedSerialPort2.Write(BuildCommand("A", left, right));
                selectedSerialPort2.Write(BuildCommand("C", left, right));
            }
            else
            {
                BuildCommand("A", left, right);
                BuildCommand("C", left, right);
            }
        }

        internal static (double left, double right) AngleToWheelSpeeds(double moveAngle, double forwardScale, bool isBlocked)
        {
            if (isBlocked) return (0, 0);

            double turn = Math.Clamp(moveAngle / 120.0, -1, 1);
            double forward = forwardScale;
            double left = forward + turn;
            double right = forward - turn;

            double maxVal = Math.Max(Math.Abs(left), Math.Abs(right));
            if (maxVal > 1)
            {
                left /= maxVal;
                right /= maxVal;
            }

            return (left, right);
        }

        internal static string BuildCommand(string axle, double leftSpeed, double rightSpeed)
        {
            int leftSpeedValue = (int)Math.Abs(Math.Clamp(leftSpeed * maxSpeed, -maxSpeed, maxSpeed));
            int rightSpeedValue = (int)Math.Abs(Math.Clamp(rightSpeed * maxSpeed, -maxSpeed, maxSpeed));

            leftSpeedValue = Math.Clamp(leftSpeedValue, minSpeed, maxSpeed);
            rightSpeedValue = Math.Clamp(rightSpeedValue, minSpeed, maxSpeed);

            string leftDirection = leftSpeed >= 0 ? "F" : "B";
            string rightDirection = rightSpeed >= 0 ? "F" : "B";

            string leftSequence = leftDirection + leftSpeedValue;
            string rightSequence = rightDirection + rightSpeedValue;

            System.Diagnostics.Debug.WriteLine($"{axle}{leftSequence}{rightSequence}{SECUREMARK}");
            return $"{axle}{leftSequence}{rightSequence}{SECUREMARK}";
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        //***************//
        //Close the ports// 
        //***************//

        internal static void ClosePorts()
        {
            if (selectedSerialPort1 != null && selectedSerialPort1.IsOpen && selectedSerialPort2 != null && selectedSerialPort2.IsOpen)
            {
                selectedSerialPort1.Close();
                selectedSerialPort2.Close();

                MessageBox.Show("Robot serial ports were closed successfully.");
            }
        }

        //************************************//
        //Turns off everything on app shutdown// 
        //************************************//

        public static void Shutdown()
        {
            if (selectedSerialPort1 != null && selectedSerialPort1.IsOpen)
            {
                selectedSerialPort1.Write("|MM0\r");  //MOTOR OFF
                selectedSerialPort1.Write("|F10\r");  //MOSFET1 OFF
                selectedSerialPort1.Write("|F20\r");  //MOSFET2 OFF

                Thread.Sleep(100);
                selectedSerialPort1.Close();
            }

            if (selectedSerialPort2 != null && selectedSerialPort2.IsOpen)
            {
                selectedSerialPort2.Close();
            }
        }
    }
}
