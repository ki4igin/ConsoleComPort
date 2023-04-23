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




    private Format _formatRx;
    private bool _statusRx;

    private readonly SerialPort _serialPort;
    private AppSettings _appSettings;
    private static Selector _selector;
    private static Request _request;

    public ComPort(AppSettings appSettings)
    {
        _appSettings = appSettings;
        _serialPort = new()
        {
            PortName = _appSettings.PortName,
            BaudRate = _appSettings.BaudRate,
            Parity =  _appSettings.Parity,
            StopBits = _appSettings.StopBits,
            ReadTimeout = 1000
        };
        _formatRx = (Format) Enum.Parse(typeof(Format), _appSettings.Format);
        DisplaySettings();

        _selector = Acc.CreateSelector(new(EscColor.ForegroundGreen, EscColor.BackgroundGreen));
        _request = Acc.CreateRequest(new(EscColor.ForegroundGreen, EscColor.ForegroundRed,
            EscColor.BackgroundDarkMagenta));
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
        Acc.WriteLine(_appSettings.ToString());
    }
}