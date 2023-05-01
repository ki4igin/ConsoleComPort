using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCompleteConsole;
using static ConsoleComPort.Info;

namespace ConsoleComPort;

public class ComPort
{
    private readonly SerialPort _serialPort;
    private readonly AppSettings _appSettings;
    private readonly ReceiveMessageParser _receiveMessageParser;
    private readonly Status _status;
    private bool _closeRequest;

    public ComPort(AppSettings appSettings, ReceiveMessageParser receiveMessageParser)
    {
        _appSettings = appSettings;
        _receiveMessageParser = receiveMessageParser;
        _appSettings.ChangedComPort += ChangedComPort;
        _appSettings.Changed += Changed;
        _status = Acc.CreateStatus();
        _serialPort = new()
        {
            PortName = _appSettings.PortName,
            BaudRate = _appSettings.BaudRate,
            Parity = _appSettings.Parity,
            StopBits = _appSettings.StopBits,
            ReadTimeout = 100
        };
        DisplaySettings();
        _status.Change(StatusStr);
        Task.Run(ReceiveProcess);
        Task.Run(UpdateStatusProcess);
    }

    private void Changed()
    {
        bool sus = _receiveMessageParser.ChangeFormat(_appSettings.Format);
        if (sus is false)
        {
            PrintWarning($"format \"{_appSettings.Format}\" is wrong");
        }
    }

    private void ChangedComPort()
    {
        bool isWasOpen = _serialPort.IsOpen;
        if (isWasOpen)
            _serialPort.Close();
        _serialPort.PortName = _appSettings.PortName;
        _serialPort.BaudRate = _appSettings.BaudRate;
        _serialPort.Parity = _appSettings.Parity;
        _serialPort.StopBits = _appSettings.StopBits;
        if (isWasOpen)
            _serialPort.Open();
    }

    public void Transmit(string message)
    {
        if (_serialPort.IsOpen == false)
        {
            return;
        }

        Acc.WriteLine(message);
        byte[] sendBytes = MessageParser.ParseMessage(message);

        if (sendBytes.Length > 0)
        {
            _serialPort.Write(sendBytes, 0, sendBytes.Length);
            string consoleStr = string.Join(" ", sendBytes.Select(b => $"0x{b:X2}"));
            PrintSendMessage(consoleStr);
        }
        else
        {
            PrintError("Format send data not correct");
        }
    }

    public void Open()
    {
        if (_serialPort.IsOpen)
            return;

        if (_serialPort.TryOpen() == false)
        {
            PrintError($"Port {_serialPort.PortName} is busy");
            return;
        }

        _serialPort.DiscardInBuffer();
        _serialPort.ReadTimeout = 100;
        PrintInfo($"Open port {_serialPort.PortName}");
    }

    public void Close()
    {
        _closeRequest = true;
    }

    public void ReOpen()
    {
        Close();
        while (_serialPort.IsOpen)
        {
        }

        Thread.Sleep(500);
        Open();
    }

    private bool TryReopenPort(int nCount)
    {
        if (_serialPort.IsOpen)
            return true;

        for (int i = 0; i < nCount; i++)
        {
            Thread.Sleep(2000);
            PrintWarning($"Try reopen port {_serialPort.PortName}, attempt {i + 1}");
            try
            {
                _serialPort.Open();
            }
            catch (Exception e)
            {
                PrintError(e.Message);
                continue;
            }
            
            PrintInfo($"Port {_serialPort.PortName} open");

            return true;
        }

        return false;
    }

    private void ReceiveProcess()
    {
        while (true)
        {
            if (_serialPort.IsOpen)
            {
                Receive();
            }

            if (_closeRequest)
            {
                _serialPort.Close();
                _closeRequest = false;
                PrintInfo($"Close port {_serialPort.PortName}");
            }

            Thread.Sleep(100);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private void Receive()
    {
        if (_serialPort.BytesToRead < Math.Max(_receiveMessageParser.BytesCount, 1))
            return;

        Debug.WriteLine($"Bytes to read {_serialPort.BytesToRead}");

        int rxCount = _receiveMessageParser.BytesCount switch
        {
            0 => _serialPort.BytesToRead,
            _ => _receiveMessageParser.BytesCount
        };

        while (_serialPort.BytesToRead >= rxCount)
        {
            byte[] bytes = new byte[rxCount];
            bool isSus = TryRead(bytes, rxCount);

            if (isSus is false)
                return;

            string str = _receiveMessageParser.Parse(bytes);
            Acc.Write(str);
        }
    }

    private bool TryRead(byte[] buffer, int count)
    {
        int rxCount = 0;
        try
        {
            do
            {
                rxCount += _serialPort.Read(buffer, rxCount, count - rxCount);
            } while (rxCount != count);

            return true;
        }
        catch (TimeoutException)
        {
        }
        catch (OperationCanceledException e)
        {
            PrintError(e.Message);
            if (TryReopenPort(3) is false)
                Close();
        }
        catch (Exception e)
        {
            PrintError(e.Message);
            Close();
        }

        return false;
    }

    public void DisplaySettings()
    {
        PrintInfo("Settings");
        Acc.WriteLine(_appSettings.ToString());
    }

    private void UpdateStatusProcess()
    {
        (string Port, int BaudRate, string Format, bool isOpen) comStatusOld =
            (_serialPort.PortName, _serialPort.BaudRate, _appSettings.Format, _serialPort.IsOpen);

        while (true)
        {
            (string Port, int BaudRate, string Format, bool isOpen) comStatusCurrent =
                (_serialPort.PortName, _serialPort.BaudRate, _appSettings.Format, _serialPort.IsOpen);
            if (comStatusOld != comStatusCurrent)
            {
                _status.Change(StatusStr);
                comStatusOld = comStatusCurrent;
            }

            Thread.Sleep(100);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private string StatusStr
    {
        get
        {
            string portNameStr = _serialPort.IsOpen switch
            {
                true => _serialPort.PortName.Color(EscColor.ForegroundDarkGreen),
                false => _serialPort.PortName.Color(EscColor.ForegroundDarkRed),
            };
            return $"Port: {portNameStr}   Baudrate: {_serialPort.BaudRate}   Format: {_appSettings.Format}";
        }
    }
}