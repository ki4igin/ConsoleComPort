using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
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
            BIN,
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
        private Settings _settings;

        public ComPort()
        {
            _settings = new Settings();
            _settings.Read();
            _serialPort = new SerialPort
            {
                PortName = _settings.PortName.Value,
                BaudRate = _settings.BaudRate.Value,
                Parity = (Parity)Enum.Parse(typeof(Parity), _settings.Parity.Value),
                DataBits = _settings.DataBits.Value,
                StopBits = (System.IO.Ports.StopBits)Enum.Parse(typeof(StopBits), _settings.StopBits.Value),
                Handshake = (Handshake)Enum.Parse(typeof(Handshake), _settings.Handshake.Value),
                ReadTimeout = 1000
            };

            Settings.Display(_settings);
        }


        public void SetAllSettings()
        {
            Settings settings = _settings;
            settings.PortName.Value = SetPortName(settings.PortName.Value);
            settings.BaudRate.Value = SetBaudRate(settings.BaudRate.Value);
            settings.Parity.Value = SetParity(settings.Parity.Value);
            settings.DataBits.Value = SetDataBits(settings.DataBits.Value);
            settings.StopBits.Value = SetStopBits(settings.StopBits.Value);
            settings.Handshake.Value = SetHandshake(settings.Handshake.Value);
            settings.Format.Value = SetFormat(settings.Format.Value);
            settings.BytesPerLine.Value = SetBytesPerLine(settings.BytesPerLine.Value);

            _serialPort.PortName = settings.PortName.Value;
            _serialPort.BaudRate = settings.BaudRate.Value;
            _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), settings.Parity.Value);
            _serialPort.DataBits = settings.DataBits.Value;
            _serialPort.StopBits = (System.IO.Ports.StopBits)Enum.Parse(typeof(StopBits), settings.StopBits.Value);
            _serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), settings.Handshake.Value);
            _formatRx = (Format)Enum.Parse(typeof(Format), settings.Format.Value);
            Settings.Display(settings);
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
                (int)Enum.Parse(typeof(Parity), currentSettings));
        private static string SetHandshake(string currentSettings) =>
            MyConsole.SelectFromList(
                Enum.GetNames(typeof(Handshake)),
                "Handshake",
                (int)Enum.Parse(typeof(Handshake), currentSettings));
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
                (int)Enum.Parse(typeof(StopBits), currentSettings));
        private static string SetFormat(string currentSettings) =>
            MyConsole.SelectFromList(
                Enum.GetNames(typeof(Format)),
                "Format",
                (int)Enum.Parse(typeof(Format), currentSettings));
        private static int SetBytesPerLine(int currentSettings) =>
            MyConsole.ReadNumber("BaudRate", currentSettings);

        public void DisplaySettings() => Settings.Display(_settings);
        public void SaveSetting() => _settings.Save();
        public void ReadSetting() => _settings.Read();
        public void SaveSettingToFile() => Settings.SaveToFile(_settings);
        public void ReadSettingFromFile()
        {
            _settings = Settings.ReadFromFile<Settings>();
            Settings.Display(_settings);
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
                MyConsole.WriteLineYellow(consoleStr);
            }
            else
            {
                MyConsole.WriteLineRed("Format send data not correct");
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
                _serialPort.Open();
            }
            _serialPort.ReadTimeout = 1000;
            MyConsole.WriteLineGreen($"\r\nStart Monitor {_serialPort.PortName}");
            _statusRX = true;
            Task.Run(() => ReceiveProcess());
        }
        public void ReceiveStop()
        {
            _statusRX = false;
            _serialPort.ReadTimeout = 100;
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
                            MyConsole.Write(Convert.ToString(value, 2) + " ");
                            break;
                        case Format.HEX:
                            MyConsole.Write($"{value:X2} ");
                            break;
                        case Format.ASCII:
                            MyConsole.Write($"{(char)value}");
                            break;
                        default:
                            break;
                    }
                    if (++cnt >= _settings.BytesPerLine.Value)
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
            MyConsole.WriteLineRed($"\r\nStop Monitor {_serialPort.PortName}");
        }

    }
}
