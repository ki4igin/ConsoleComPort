using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCompleteConsole;
using static ConsoleComPort.Info;

namespace ConsoleComPort;

public class ComPort
{
    private const int PacketSize = 0;

    private readonly SerialPort _serialPort;
    private readonly AppSettings _appSettings;
    private bool _closeRequest;

    public ComPort(AppSettings appSettings)
    {
        _appSettings = appSettings;
        _appSettings.ChangedComPort += ChangedComPort;
        _serialPort = new()
        {
            PortName = _appSettings.PortName,
            BaudRate = _appSettings.BaudRate,
            Parity = _appSettings.Parity,
            StopBits = _appSettings.StopBits,
            ReadTimeout = 1000
        };
        DisplaySettings();
        Task.Run(ReceiveProcess);
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
            Acc.WriteLine(consoleStr, EscColor.ForegroundYellow);
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
        _serialPort.ReadTimeout = 1000;
        Acc.WriteLine($"Open port {_serialPort.PortName}", EscColor.ForegroundGreen);
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
        while (true)
        {
            if (_serialPort.IsOpen)
            {
                Receive();
            }

            if (_closeRequest)
                _serialPort.Close();

            Task.Delay(100);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private void Receive()
    {
        if (_serialPort.BytesToRead < PacketSize)
            return;

        int rxCount = PacketSize switch
        {
            0 => _serialPort.BytesToRead,
            _ => PacketSize
        };

        byte[] bytes = new byte[rxCount];
        bool isSus = TryRead(bytes, rxCount);
        
        if (isSus is false)
            return;

        string str = ParseMessage(bytes);
        Acc.Write(str);
    }

    private string ParseMessage(byte[] bytes)
    {
        return string.Join(" ", bytes.Select(b => $"0x{b:X2}"));
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
        Acc.WriteLine("Current Settings:", EscColor.ForegroundGreen);
        Acc.WriteLine(_appSettings.ToString());
    }
}