using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCompleteConsole;
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


    private Format _formatRx;
    private bool _statusRx;

    private readonly SerialPort _serialPort;
    private AppSettings _appSettings;
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
        _formatRx = (Format) Enum.Parse(typeof(Format), _appSettings.Format);
        DisplaySettings();
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
        Task.Run(ReceiveProcess);
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
        int cnt = 0;
       
        while (true)
        {
            try
            {
                _serialPort.ReadTimeout = 10;
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
                    Close();
            }
            catch (Exception e)
            {
                PrintError(e.Message);
                Close();
            }
        }

        _serialPort.Close();
        Acc.WriteLine($"Stop Monitor {_serialPort.PortName}", EscColor.ForegroundGreen);
    }

    public void DisplaySettings()
    {
        Acc.WriteLine("Current Settings:", EscColor.ForegroundGreen);
        Acc.WriteLine(_appSettings.ToString());
    }
}