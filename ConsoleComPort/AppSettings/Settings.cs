using AppTools;
using System.ComponentModel;

namespace AppSettings
{
    public class Settings : IDescription, ISettings
    {
        [Description("Port")] public string PortName { get; set; } = "COM1";
        [Description("BaudRate")] public int BaudRate { get; set; } = 9600;
        [Description("Parity")] public string Parity { get; set; } = "None";
        [Description("DataBits")] public int DataBits { get; set; } = 8;
        [Description("StopBits")] public string StopBits { get; set; } = "One";
        [Description("Handshake")] public string Handshake { get; set; } = "None";
        [Description("Format Receive")] public string Format { get; set; } = "ASCII";
        [Description("Bytes per line")] public int BytesPerLine { get; set; } = 500;
    }
}






