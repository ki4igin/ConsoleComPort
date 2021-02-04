using AppSettings;
using AppTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleComPort
{
    public class ComPort
    {
        public enum StopBits
        {
            One = 1,
            Two = 2,
            OnePointFive = 3
        }
        public enum Format
        {
            BIN = 0,
            HEX,
            ASCII
        }
        readonly string[] _baudRatesList =
        {
            "4800",
            "9600",
            "115200",
            "Other"
        };
        readonly string[] _dataBitsList =
        {
            "5",
            "6",
            "7",
            "8"
        };

        Format _formatRx = Format.ASCII;
        bool _statusRX = false;

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
                Parity = (Parity)Enum.Parse(typeof(Parity), _settings.Parity),
                DataBits = _settings.DataBits,
                StopBits = (System.IO.Ports.StopBits)Enum.Parse(typeof(StopBits), _settings.StopBits),
                Handshake = (Handshake)Enum.Parse(typeof(Handshake), _settings.Handshake),
                ReadTimeout = 1000
            };
            _formatRx = (Format)Enum.Parse(typeof(Format), _settings.Format);
            _settings.Display();
        }

        public void SetAllSettings()
        {
            if (_statusRX)
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
            _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), settings.Parity);
            _serialPort.DataBits = settings.DataBits;
            _serialPort.StopBits = (System.IO.Ports.StopBits)Enum.Parse(typeof(StopBits), settings.StopBits);
            _serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), settings.Handshake);
            _formatRx = (Format)Enum.Parse(typeof(Format), settings.Format);
            _settings.Display();
        }

        private static string SetPortName(string currentSettings)
        {
            var portNames = SerialPort.GetPortNames();
            if (portNames.Length == 0)
            {
                return "None";
            }
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
            if (baudRateStr == _baudRatesList.Last())
            {
                baudRate = MyConsole.ReadNumber("BaudRate", baudRate);
            }
            else
            {
                baudRate = int.Parse(baudRateStr);
            }
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
            MyConsole.ReadNumber("BaudRate", currentSettings);

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
            if (_statusRX == false)
            {
                return;
            }

            string sendStr = default;
            string consoleStr = default;
            if (message.Length >= 2 && message[..2] == "0x")
            {
                message = message
                    .Replace("_", "")
                    .Replace("0x", "")
                    .Replace(" ", "")
                    .Replace("-", "")
                    .ToUpper();
                List<char> listByte = new();
                List<string> listStrs = new();
                for (int i = 0; i < message.Length - 1; i += 2)
                {
                    var strByte = message[i..(i + 2)];
                    if (byte.TryParse(strByte, NumberStyles.HexNumber, null, out byte value))
                    {
                        listByte.Add((char)value);
                        listStrs.Add(strByte);
                    }
                    sendStr = new string(listByte.ToArray());
                    consoleStr = string.Join(" ", listStrs.Select(str => $"0x{str}"));
                }
            }
            else
            {
                sendStr = message;
                consoleStr = message;
            }

            if (sendStr.Length > 0)
            {
                _serialPort.Write(sendStr);
                MyConsole.WriteNewLineYellow(consoleStr);
            }
            else
            {
                MyConsole.WriteNewLineRed("Format send data not correct");
            }

        }
        public void ReceiveStart()
        {
            if (_statusRX)
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
            _statusRX = true;
            Task.Run(() => ReceiveProcess());
        }
        public void ReceiveStop()
        {
            _statusRX = false;
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
            while (_statusRX)
            {
                try
                {
                    int value = _serialPort.ReadByte();
                    switch (_formatRx)
                    {
                        case Format.BIN:
                            MyConsole.Write($"0b{Convert.ToString(value, 2)} ");
                            break;
                        case Format.HEX:
                            MyConsole.Write($"0x{value:X2} ");
                            break;
                        case Format.ASCII:
                            MyConsole.Write($"{(char)value}");
                            break;
                        default:
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
            }
            _serialPort.Close();
            MyConsole.WriteNewLineRed($"Stop Monitor {_serialPort.PortName}");
        }

    }
}
