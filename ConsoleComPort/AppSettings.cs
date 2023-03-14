using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AppTools;
using YamlDotNet.Serialization;

namespace ConsoleComPort;

public class AppSettings : IDescription
{
    private const string SettingsFile = "appsettings.yml";
        
    [Description("Port")] public string PortName { get; set; } = "COM1";
    [Description("BaudRate")] public int BaudRate { get; set; } = 9600;
    [Description("Parity")] public string Parity { get; set; } = "None";
    [Description("DataBits")] public int DataBits { get; set; } = 8;
    [Description("StopBits")] public string StopBits { get; set; } = "One";
    [Description("Handshake")] public string Handshake { get; set; } = "None";
    [Description("Format Receive")] public string Format { get; set; } = "Ascii";
    [Description("Bytes per line")] public int BytesPerLine { get; set; } = 500;

    public static AppSettings Read()
    {
        if (File.Exists(SettingsFile) is false)
            return new();
        
        IDeserializer deserializer = new DeserializerBuilder().Build();
        using StreamReader reader = new(SettingsFile);
        return deserializer.Deserialize<AppSettings>(reader);
    }

    public static void Save(AppSettings settings)
    {
        ISerializer serializer = new SerializerBuilder().Build();
        string yaml = serializer.Serialize(settings);
        File.WriteAllText(SettingsFile, yaml);
    }

    // private AppSettings(){}

    public string GetString() =>
        string.Join(
            Environment.NewLine,
            GetType()
                .GetProperties()
                .Select(p => $"{this.GetPropertyDescription(p.Name),-40} {p.GetValue(this)}")
        );
}