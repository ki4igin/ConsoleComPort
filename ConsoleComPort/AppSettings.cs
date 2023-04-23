using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using AppTools;
using AutoCompleteConsole;
using AutoCompleteConsole.StringProvider;
using YamlDotNet.Serialization;

namespace ConsoleComPort;

public class AppSettings : IDescription
{
    private const string SettingsFile = "appsettings.yml";

    private readonly Selector _selector;
    private readonly Request _request;

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

    [Description("Port")] public string PortName { get; private set; } = "COM1";
    [Description("BaudRate")] public int BaudRate { get; private set; } = 9600;
    [Description("Parity")] public Parity Parity { get; private set; } = Parity.None;
    [Description("StopBits")] public StopBits StopBits { get; private set; } = StopBits.One;
    [Description("Format Receive")] public string Format { get; set; } = "Ascii";
    [Description("Bytes per line")] public int BytesPerLine { get; private set; } = 500;

    [YamlIgnore] internal Action? Changed { get; set; }

    public AppSettings()
    {
        _selector = Acc.CreateSelector(new(EscColor.ForegroundGreen, EscColor.BackgroundGreen));
        _request = Acc.CreateRequest(new(EscColor.ForegroundGreen, EscColor.ForegroundRed,
            EscColor.BackgroundDarkMagenta));
    }

    public static AppSettings Load()
    {
        IDeserializer deserializer = new DeserializerBuilder().Build();
        try
        {
            using StreamReader reader = new(SettingsFile);
            return deserializer.Deserialize<AppSettings>(reader);
        }
        catch (Exception e)
        {
            Info.PrintWarning(SettingsFile + " " + e);
            return new();
        }
    }

    public static void Save(AppSettings settings)
    {
        ISerializer serializer = new SerializerBuilder().Build();
        string yaml = serializer.Serialize(settings);
        File.WriteAllText(SettingsFile, yaml);
    }

    public override string ToString()
    {
        PropertyInfo[] prop = GetType().GetProperties();
        int maxLengthPropName = prop.Select(p => this.GetPropertyDescription(p.Name).Length).Max();
        return string.Join(
            Environment.NewLine,
            prop
                .Select(p =>
                    $"{this.GetPropertyDescription(p.Name).PadRight(maxLengthPropName + 3)} {p.GetValue(this)}")
        );
    }

    public void SetAllSettings()
    {
        int countChanges = 0;

        string portName = GetNewPortName();
        if (PortName != portName)
        {
            PortName = portName;
            countChanges++;
        }

        int baudRate = GetNewBaudRate();
        if (BaudRate != baudRate)
        {
            BaudRate = baudRate;
            countChanges++;
        }

        Parity parity = GetNewParity();
        if (Parity != parity)
        {
            Parity = parity;
            countChanges++;
        }

        StopBits stopBits = GetNewStopBits();
        if (StopBits != stopBits)
        {
            StopBits = stopBits;
            countChanges++;
        }

        int bytesPerLine = GetNewBytesPerLine();
        if (BytesPerLine != bytesPerLine)
        {
            BytesPerLine = bytesPerLine;
            countChanges++;
        }

        if (countChanges > 0)
            Changed?.Invoke();
    }

    public void SetPortName()
    {
        string portName = GetNewPortName();
        if (PortName != portName)
        {
            PortName = portName;
            Changed?.Invoke();
        }
    }

    public void SetBaudRate()
    {
        int baudRate = GetNewBaudRate();
        if (BaudRate != baudRate)
        {
            BaudRate = baudRate;
            Changed?.Invoke();
        }
    }

    public void SetParty()
    {
        Parity parity = GetNewParity();
        if (Parity != parity)
        {
            Parity = parity;
            Changed?.Invoke();
        }
    }

    public void SetStopBits()
    {
        StopBits stopBits = GetNewStopBits();
        if (StopBits != stopBits)
        {
            StopBits = stopBits;
            Changed?.Invoke();
        }
    }

    public void SetBytesPerLine()
    {
        int bytesPerLine = GetNewBytesPerLine();
        if (BytesPerLine != bytesPerLine)
        {
            BytesPerLine = bytesPerLine;
            Changed?.Invoke();
        }
    }


    private string GetNewPortName()
    {
        string[]? portNames = SerialPort.GetPortNames();
        if (portNames.Length == 0)
        {
            return PortName;
        }

        List<string> portNamesList = new();

        foreach (string? portName in portNames)
        {
            SerialPort serialPort = new(portName, 115200);
            if (serialPort.TryOpen())
            {
                portNamesList.Add(portName);
                serialPort.Close();
            }
        }

        portNames = portNamesList.OrderBy(p => Convert.ToInt32(p.Remove(0, 3))).Distinct().ToArray();

        int ind = portNames.ToList().IndexOf(PortName);
        ind = ind >= 0 ? ind : 0;

        return _selector.Run(new("Ports", portNames), ind);
    }

    private int GetNewBaudRate()
    {
        int ind = _baudRatesList.ToList().IndexOf(BaudRate.ToString());
        ind = ind >= 0 ? ind : 0;
        string baudRateStr = _selector.Run(new("BaudRate", _baudRatesList), ind);
        int baudRate = int.Parse(_baudRatesList[ind]);
        return baudRateStr == _baudRatesList.Last()
            ? int.Parse(_request.ReadLine(
                new("BaudRate", "Must be an integer"),
                s => int.TryParse(s, out int _),
                baudRate.ToString()))
            : int.Parse(baudRateStr);
    }

    private Parity GetNewParity() =>
        Enum.Parse<Parity>(_selector.Run(
            new("Parity", Enum.GetNames(typeof(Parity))[..3]),
            Enum.GetNames(typeof(Parity))[..3].ToList().IndexOf(Parity.GetDescription())));

    private StopBits GetNewStopBits() =>
        Enum.Parse<StopBits>(_selector.Run(
            new("StopBits", Enum.GetNames(typeof(StopBits))[1..]),
            Enum.GetNames(typeof(StopBits))[1..].ToList().IndexOf(StopBits.GetDescription())));

    // private void SetFormat() =>
    //     Format = _selector.Run(
    //         new("Format", Enum.GetNames(typeof(Format))),
    //         Enum.GetNames(typeof(Format)).ToList().IndexOf(Format));

    private int GetNewBytesPerLine() =>
        int.Parse(_request.ReadLine(
            new("BytesPerLine", "Must be an integer"),
            s => int.TryParse(s, out int _),
            BytesPerLine.ToString()));
}