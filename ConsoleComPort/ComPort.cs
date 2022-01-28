﻿using AppSettings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleComPort.AppTools;

namespace ConsoleComPort
{
    public class ComPort
    {
        private enum Format
        {
            Bin = 0,
            Hex,
            Ascii
        }

        readonly string[] _baudRatesList =
        {
            "4800",
            "9600",
            "19200",
            "38400",
            "57600",
            "115200",
            "128000",
            "256000",
            "Other"
        };

        readonly string[] _dataBitsList =
        {
            "5",
            "6",
            "7",
            "8"
        };

        Format _formatRx;
        bool _statusRx;

        private readonly SerialPort _serialPort;
        private readonly Settings _settings;

        public ComPort()
        {
            _settings = new Settings();
            _settings.Read();
            _serialPort = new SerialPort
            {
                PortName = _settings.PortName,
                BaudRate = _settings.BaudRate,
                Parity = (Parity) Enum.Parse(typeof(Parity), _settings.Parity),
                DataBits = _settings.DataBits,
                StopBits = (StopBits) Enum.Parse(typeof(StopBits), _settings.StopBits),
                Handshake = (Handshake) Enum.Parse(typeof(Handshake), _settings.Handshake),
                ReadTimeout = 1000
            };
            _formatRx = (Format) Enum.Parse(typeof(Format), _settings.Format);
            _settings.Display();
        }

        public void SetAllSettings()
        {
            if (_statusRx)
            {
                MyConsole.WriteNewLineRed("First stop monitor");
                return;
            }

            Settings settings = _settings;
            settings.PortName = SetPortName(settings.PortName);
            settings.BaudRate = SetBaudRate(settings.BaudRate);
            settings.Parity = SetParity(settings.Parity);
            settings.DataBits = SetDataBits(settings.DataBits);
            settings.StopBits = SetStopBits(settings.StopBits);
            settings.Handshake = SetHandshake(settings.Handshake);
            settings.Format = SetFormat(settings.Format);
            settings.BytesPerLine = SetBytesPerLine(settings.BytesPerLine);

            _serialPort.PortName = settings.PortName;
            _serialPort.BaudRate = settings.BaudRate;
            _serialPort.Parity = (Parity) Enum.Parse(typeof(Parity), settings.Parity);
            _serialPort.DataBits = settings.DataBits;
            _serialPort.StopBits = (StopBits) Enum.Parse(typeof(StopBits), settings.StopBits);
            _serialPort.Handshake = (Handshake) Enum.Parse(typeof(Handshake), settings.Handshake);
            _formatRx = (Format) Enum.Parse(typeof(Format), settings.Format);
            _settings.Display();
        }

        private static string SetPortName(string currentSettings)
        {
            var portNames = SerialPort.GetPortNames();
            if (portNames.Length == 0)
            {
                return "None";
            }

            List<string> portNamesList = new();

            foreach (var portName in portNames)
            {
                SerialPort serialPort = new(portName);
                try
                {
                    serialPort.Open();
                }
                catch (UnauthorizedAccessException)
                {
                    serialPort.Close();
                    continue;
                }
                catch (System.IO.FileNotFoundException)
                {
                    serialPort.Close();
                    continue;
                }

                portNamesList.Add(portName);
                serialPort.Close();
            }

            portNames = portNamesList.OrderBy(p => Convert.ToInt32(p.Remove(0, 3))).Distinct().ToArray();

            var ind = portNames.ToList().IndexOf(currentSettings);
            ind = ind >= 0 ? ind : 0;
            return MyConsole.SelectFromList(portNames, "Port", ind);
        }

        private int SetBaudRate(int currentSettings)
        {
            var ind = _baudRatesList.ToList().IndexOf(currentSettings.ToString());
            ind = ind >= 0 ? ind : 0;
            var baudRateStr = MyConsole.SelectFromList(_baudRatesList, "BaudRate", ind);
            var baudRate = int.Parse(_baudRatesList[ind]);
            baudRate = baudRateStr == _baudRatesList.Last()
                ? MyConsole.ReadNumber("BaudRate", baudRate)
                : int.Parse(baudRateStr);

            return baudRate;
        }

        private static string SetParity(string currentSettings) =>
            MyConsole.SelectFromList(
                Enum.GetNames(typeof(Parity)),
                "Parity",
                Enum.GetNames(typeof(Parity)).ToList().IndexOf(currentSettings));

        private static string SetHandshake(string currentSettings) =>
            MyConsole.SelectFromList(
                Enum.GetNames(typeof(Handshake)),
                "Handshake",
                Enum.GetNames(typeof(Handshake)).ToList().IndexOf(currentSettings));

        private int SetDataBits(int currentSettings)
        {
            var ind = _dataBitsList.ToList().IndexOf(currentSettings.ToString());
            ind = ind >= 0 ? ind : 0;
            var str = MyConsole.SelectFromList(_dataBitsList, "BaudRate", ind);
            return int.Parse(str);
        }

        private static string SetStopBits(string currentSettings) =>
            MyConsole.SelectFromList(
                Enum.GetNames(typeof(StopBits)),
                "StopBits",
                Enum.GetNames(typeof(StopBits)).ToList().IndexOf(currentSettings));

        private static string SetFormat(string currentSettings) =>
            MyConsole.SelectFromList(
                Enum.GetNames(typeof(Format)),
                "Format",
                Enum.GetNames(typeof(Format)).ToList().IndexOf(currentSettings));

        private static int SetBytesPerLine(int currentSettings) =>
            MyConsole.ReadNumber("BytesPerLine", currentSettings);

        public void DisplaySettings() => _settings.Display();
        public void SaveSetting() => _settings.Save();
        public void ReadSetting() => _settings.Read();
        public void SaveSettingToFile() => _settings.SaveToFile();

        public void ReadSettingFromFile()
        {
            _settings.ReadFromFile();
            _settings.Display();
        }

        public void Transmit(string message)
        {
            if (_statusRx == false)
            {
                return;
            }

            byte[] sendBytes = default;
            string consoleStr = default;
            if (message.Length >= 2 && message[..2] == "0x")
            {
                message = message
                    .Replace("_", "")
                    .Replace("0x", "")
                    .Replace(" ", "")
                    .Replace("-", "")
                    .ToUpper();
                List<byte> listByte = new();
                List<string> listStrings = new();
                for (var i = 0; i < message.Length - 1; i += 2)
                {
                    var strByte = message[i..(i + 2)];
                    if (byte.TryParse(strByte, NumberStyles.HexNumber, null, out byte value))
                    {
                        listByte.Add(value);
                        listStrings.Add(strByte);
                    }
                    
                }
                sendBytes = listByte.ToArray();
                consoleStr = string.Join(" ", listStrings.Select(str => $"0x{str}"));
            }
            else if (message.Length >= 10 && message[..2] == "0b")
            {
                message = message
                    .Replace("_", "")
                    .Replace("0b", "")
                    .Replace(" ", "")
                    .Replace("-", "")
                    .ToUpper();
                List<byte> listByte = new();
                List<string> listStrings = new();
                for (var i = 0; i < message.Length - 1; i += 8)
                {
                    var strByte = message[i..(i + 8)];

                    try
                    {
                        byte value = Convert.ToByte(strByte, 2);
                        listByte.Add(value);
                        listStrings.Add(strByte);
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine(e);
                    }
                }
                sendBytes = listByte.ToArray();
                consoleStr = string.Join(" ", listStrings.Select(str => $"0b{str}"));
            }
            else
            {
                sendBytes = Encoding.ASCII.GetBytes(message);
                consoleStr = message;
            }

            if (sendBytes is {Length: > 0})
            {
                _serialPort.Write(sendBytes, 0, sendBytes.Length);
                MyConsole.WriteNewLineYellow(consoleStr);
            }
            else
            {
                MyConsole.WriteNewLineRed("Format send data not correct");
            }
        }

        public void ReceiveStart()
        {
            if (_statusRx)
            {
                return;
            }

            if (_serialPort.IsOpen == false)
            {
                try
                {
                    _serialPort.Open();
                }
                catch (UnauthorizedAccessException)
                {
                    MyConsole.WriteNewLineRed($"Port is Busy");
                    return;
                }
            }

            _serialPort.ReadTimeout = 1000;
            MyConsole.WriteNewLineGreen($"Start Monitor {_serialPort.PortName}");
            _statusRx = true;
            Task.Run(ReceiveProcess);
        }

        public void ReceiveStop()
        {
            _statusRx = false;
            _serialPort.ReadTimeout = 100;
        }

        public void ReceiveReboot()
        {
            ReceiveStop();
            while (_serialPort.IsOpen)
            {
            }

            Thread.Sleep(500);
            ReceiveStart();
        }

        private void ReceiveProcess()
        {
            int cnt = 0;
            _serialPort.DiscardInBuffer();
            while (_statusRx)
            {
                try
                {
                    int value = _serialPort.ReadByte();
                    switch (_formatRx)
                    {
                        case Format.Bin:
                            MyConsole.Write($"0b{Convert.ToString(value, 2)} ");
                            break;
                        case Format.Hex:
                            MyConsole.Write($"0x{value:X2} ");
                            break;
                        case Format.Ascii:
                            if (value == '\n')
                            {
                                cnt = 0;
                            }

                            MyConsole.Write($"{(char) value}");
                            break;
                    }

                    if (++cnt >= _settings.BytesPerLine)
                    {
                        MyConsole.WriteLine();
                        cnt = 0;
                    }
                }
                catch (TimeoutException)
                {
                }
                catch (Exception e)
                {
                    MyConsole.WriteLineRed(e.Message);
                    ReceiveStop();
                }
            }

            _serialPort.Close();
            MyConsole.WriteNewLineRed($"Stop Monitor {_serialPort.PortName}");
        }
    }
}