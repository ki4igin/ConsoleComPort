using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCompleteConsole;
using AutoCompleteConsole.StringProvider;
using static ConsoleComPort.Info;

namespace ConsoleComPort;

public class ComPort
{
    private enum Format
    {
        Bin = 0,
        Hex,
        Ascii
    }

    private readonly string[] _baudRatesList =
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


    private Format _formatRx;
    private bool _statusRx;

    private readonly SerialPort _serialPort;
    private AppSettings _appSettings;
    private static Selector _selector;
    private static Request _request;

    public ComPort()
    {
        _appSettings = AppSettings.Read();
        _serialPort = new()
        {
            PortName = _appSettings.PortName,
            BaudRate = _appSettings.BaudRate,
            Parity = (Parity) Enum.Parse(typeof(Parity), _appSettings.Parity),
            StopBits = (StopBits) Enum.Parse(typeof(StopBits), _appSettings.StopBits),
            ReadTimeout = 1000
        };
        _formatRx = (Format) Enum.Parse(typeof(Format), _appSettings.Format);
        DisplaySettings();

        _selector = Acc.CreateSelector(new(EscColor.ForegroundGreen, EscColor.BackgroundGreen));
        _request = Acc.CreateRequest(new(EscColor.ForegroundGreen, EscColor.ForegroundRed,
            EscColor.BackgroundDarkMagenta));
    }

    public void SetAllSettings()
    {
        if (_statusRx)
        {
            PrintError("First stop monitor");
            return;
        }

        _appSettings.PortName = SetPortName(_appSettings.PortName);
        _appSettings.BaudRate = SetBaudRate(_appSettings.BaudRate);
        _appSettings.Parity = SetParity(_appSettings.Parity);
        _appSettings.StopBits = SetStopBits(_appSettings.StopBits);
        _appSettings.Format = SetFormat(_appSettings.Format);
        _appSettings.BytesPerLine = SetBytesPerLine(_appSettings.BytesPerLine);

        _serialPort.PortName = _appSettings.PortName;
        _serialPort.BaudRate = _appSettings.BaudRate;
        _serialPort.Parity = (Parity) Enum.Parse(typeof(Parity), _appSettings.Parity);
        _serialPort.StopBits = (StopBits) Enum.Parse(typeof(StopBits), _appSettings.StopBits);
        _formatRx = (Format) Enum.Parse(typeof(Format), _appSettings.Format);
        DisplaySettings();
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
            SerialPort serialPort = new(portName, 115200);
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
            catch (System.IO.IOException)
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
        return _selector.Run(new("Port", portNames), ind);
    }

    private int SetBaudRate(int currentSettings)
    {
        var ind = _baudRatesList.ToList().IndexOf(currentSettings.ToString());
        ind = ind >= 0 ? ind : 0;
        var baudRateStr = _selector.Run(new("BaudRate", _baudRatesList), ind);
        var baudRate = int.Parse(_baudRatesList[ind]);
        baudRate = baudRateStr == _baudRatesList.Last()
            ? int.Parse(_request.ReadLine(
                new("BaudRate", "Must be an integer"),
                s => int.TryParse(s, out int _),
                baudRate.ToString()))
            : int.Parse(baudRateStr);

        return baudRate;
    }

    private static string SetParity(string currentSettings) =>
        _selector.Run(
            new("Parity", Enum.GetNames(typeof(Parity))),
            Enum.GetNames(typeof(Parity)).ToList().IndexOf(currentSettings));

    private static string SetStopBits(string currentSettings) =>
        _selector.Run(
            new("StopBits", Enum.GetNames(typeof(StopBits))),
            Enum.GetNames(typeof(StopBits)).ToList().IndexOf(currentSettings));

    private static string SetFormat(string currentSettings) =>
        _selector.Run(
            new("Format", Enum.GetNames(typeof(Format))),
            Enum.GetNames(typeof(Format)).ToList().IndexOf(currentSettings));

    private static int SetBytesPerLine(int currentSettings) =>
        int.Parse(_request.ReadLine(
            new("BytesPerLine", "Must be an integer"),
            s => int.TryParse(s, out int _),
            currentSettings.ToString()));

    public void SaveSetting() => AppSettings.Save(_appSettings);
    public void ReadSetting() => _appSettings = AppSettings.Read();


    public void Transmit(string message)
    {
        if (_statusRx == false)
        {
            return;
        }

        Acc.WriteLine(message);
        byte[] sendBytes = MessageParser.ParseMessage(message);

        if (sendBytes is {Length: > 0})
        {
            _serialPort.Write(sendBytes, 0, sendBytes.Length);
            string consoleStr = string.Join(" ", sendBytes.Select(b => $"0x{b:X2}"));
            Acc.WriteLine(consoleStr, EscColor.ForegroundYellow);
        }
        else
        {
            PrintError("Format send data not correct");
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
                PrintError("Port is Busy");
                return;
            }
        }

        _serialPort.ReadTimeout = 1000;
        Acc.WriteLine($"Start Monitor {_serialPort.PortName}", EscColor.ForegroundGreen);
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

    private bool TryReopenPort(int nCount)
    {
        if (_serialPort.IsOpen)
            return true;

        for (int i = 0; i < nCount; i++)
        {
            Thread.Sleep(2000);
            Acc.WriteLine($"Try reopen port {_serialPort.PortName}, attempt {i + 1}", EscColor.ForegroundYellow);
            try
            {
                _serialPort.Open();
            }
            catch (Exception e)
            {
                PrintError(e.Message);
                continue;
            }
                
            Acc.WriteLine($"Port {_serialPort.PortName} open", EscColor.ForegroundGreen);

            return true;
        }

        return false;
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
                        Acc.Write($"0b{Convert.ToString(value, 2)} ");
                        break;
                    case Format.Hex:
                        Acc.Write($"0x{value:X2} ");
                        break;
                    case Format.Ascii:
                        if (value == '\n')
                        {
                            cnt = 0;
                        }

                        Acc.Write($"{(char) value}");
                        break;
                }

                if (++cnt >= _appSettings.BytesPerLine)
                {
                    Acc.WriteLine();
                    cnt = 0;
                }
            }
            catch (TimeoutException)
            {
            }
            catch (OperationCanceledException e)
            {
                PrintError(e.Message);
                if (TryReopenPort(3) is false)
                    ReceiveStop();
            }
            catch (Exception e)
            {
                PrintError(e.Message);
                ReceiveStop();
            }
        }

        _serialPort.Close();
        Acc.WriteLine($"Stop Monitor {_serialPort.PortName}", EscColor.ForegroundGreen);
    }

    public void DisplaySettings()
    {
        Acc.WriteLine("Current Settings:", EscColor.ForegroundGreen);
        Acc.WriteLine(_appSettings.GetString());
    }
}