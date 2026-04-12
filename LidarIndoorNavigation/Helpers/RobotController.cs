using LidarIndoorNavigation.Models;
using System.IO.Ports;
using System.Windows;

namespace LidarIndoorNavigation.Helpers
{
    public class RobotController
    {
        //Odber = 1.2A, pri zmene smeru 1.8A

        private static SerialPort? selectedSerialPort1; //Seriová linka pre Motory
        private static SerialPort? selectedSerialPort2; //Seriová linka pre Napájanie
        private static string SECUREMARK = "*";
        private static string maxSpeedString = "5000";
        private static bool Electronic = false;
        private static bool Engine = false;

        //***************************************//
        //Nastavenie seriovej linky pre Napájanie//
        //***************************************//

        public (bool result, string? message) OpenSerialPort1(string portName)
        {
            if (!string.IsNullOrEmpty(portName))
            {
                selectedSerialPort1 = new SerialPort(portName);
                selectedSerialPort1.BaudRate = 19200;
                selectedSerialPort1.DataBits = 8;
                selectedSerialPort1.Parity = Parity.None;
                selectedSerialPort1.StopBits = StopBits.One;
                try
                {
                    selectedSerialPort1.Open();
                    return (selectedSerialPort1.IsOpen, null);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }

            return (false, "Port name is null or empty");
        }

        //************************************//
        //Nastavenie seriovej linky pre Motory//
        //************************************//

        public (bool result, string? message) OpenSerialPort2(string portName)
        {
            if (!string.IsNullOrEmpty(portName))
            {
                selectedSerialPort2 = new SerialPort(portName);
                selectedSerialPort2.BaudRate = 115200;
                selectedSerialPort2.DataBits = 8;
                selectedSerialPort2.Parity = Parity.None;
                selectedSerialPort2.StopBits = StopBits.One;
                try
                {
                    selectedSerialPort2.Open();
                    return (selectedSerialPort2.IsOpen, null);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }

            return (false, "Port name is null or empty");
        }

        //**********************************************//    //*************//
        //Tlačídlo MOSFET pre zapnutie MOSFET1 a MOSFET2//    //Serial Port 2//
        //**********************************************//    //*************//

        public bool ElectronicButton()
        {
            if (selectedSerialPort1 != null && selectedSerialPort2 != null && selectedSerialPort1.IsOpen && selectedSerialPort2.IsOpen)
            {
                if (Electronic == false)
                {
                    selectedSerialPort1.Write("|F11\r");  //MOSFET1 ON
                    selectedSerialPort1.Write("|F21\r");  //MOSFET2 ON
                    Electronic = true;
                    return Electronic;
                }
                else
                {
                    selectedSerialPort1.Write("|F10\r");  //MOSFET1 OFF
                    selectedSerialPort1.Write("|F20\r");  //MOSFET2 OFF
                    Electronic = false;
                    return Electronic;
                }
            }

            return Electronic;
        }

        //**************************************//    //*************//
        //Tlačídlo Motory pre zapnutie  motorov//    //Serial Port 2//
        //************************************//    //*************//

        public bool EngineButton()
        {
            if (selectedSerialPort1 != null && selectedSerialPort2 != null && selectedSerialPort1.IsOpen && selectedSerialPort2.IsOpen)
            {

                if (Electronic == true && Engine == true)
                {

                    selectedSerialPort1.Write("|MM0\r");  //MOTOR OFF
                    Engine = false;
                    return Engine;
                }

                else if (Electronic == true && Engine == false)
                {
                    selectedSerialPort1.Write("|MM1\r");  //MOTOR ON
                    Engine = true;
                    return Engine;
                }
            }

            return Engine;
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
                        string controlCommand = ("A" + "F" + maxSpeedString + "F" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + maxSpeedString + "F" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);
                    }

                    if (command == MovementCommands.Backward)   /*BACKWARDS*/
                    {
                        string controlCommand = ("A" + "B" + maxSpeedString + "B" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "B" + maxSpeedString + "B" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);
                    }

                    if (command == MovementCommands.TurnLeft)   /*LEFT*/
                    {
                        string controlCommand = ("A" + "B" + maxSpeedString + "F" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "B" + maxSpeedString + "F" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);
                    }

                    if (command == MovementCommands.TurnRight)   /*RIGHT*/
                    {

                        string controlCommand = ("A" + "F" + maxSpeedString + "B" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + maxSpeedString + "B" + maxSpeedString + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);
                    }

                    if (command == MovementCommands.Stop)   /*STOP*/
                    {

                        string controlCommand = ("A" + "F" + "0000" + "F" + "0000" + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand);

                        string controlCommand2 = ("C" + "F" + "0000" + "F" + "0000" + SECUREMARK);
                        selectedSerialPort2.Write(controlCommand2);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error sending string to robot: " + ex.Message);
                }
            }
        }

        //***************//
        //Close the ports// 
        //***************//

        internal (bool port1, bool port2) ClosePorts()
        {
            if (selectedSerialPort1 != null && selectedSerialPort1.IsOpen && selectedSerialPort2 != null && selectedSerialPort2.IsOpen)
            {
                selectedSerialPort1.Close();
                selectedSerialPort2.Close();

                return (selectedSerialPort1.IsOpen, selectedSerialPort2.IsOpen);
            }

            return (false, false);
        }

        //************************************//
        //Turns off everything on app shutdown// 
        //************************************//

        public void Shutdown()
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
