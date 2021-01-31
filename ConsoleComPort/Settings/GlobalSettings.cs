using System.Configuration;
using System.IO.Ports;

sealed class GlobalSettings : ApplicationSettingsBase
{
	[UserScopedSetting()]
    [DefaultSettingValue("COM1")]
    public System.String PortName
    {
        get => (System.String)this[nameof(PortName)];
        set => this[nameof(PortName)] = value;
    }
	[UserScopedSetting()]
    [DefaultSettingValue("9600")]
    public System.Int32 BaudRate
    {
        get => (System.Int32)this[nameof(BaudRate)];
        set => this[nameof(BaudRate)] = value;
    }
	[UserScopedSetting()]
    [DefaultSettingValue("None")]
    public System.String Parity
    {
        get => (System.String)this[nameof(Parity)];
        set => this[nameof(Parity)] = value;
    }
	[UserScopedSetting()]
    [DefaultSettingValue("8")]
    public System.Int32 DataBits
    {
        get => (System.Int32)this[nameof(DataBits)];
        set => this[nameof(DataBits)] = value;
    }
	[UserScopedSetting()]
    [DefaultSettingValue("One")]
    public System.String StopBits
    {
        get => (System.String)this[nameof(StopBits)];
        set => this[nameof(StopBits)] = value;
    }
	[UserScopedSetting()]
    [DefaultSettingValue("None")]
    public System.String Handshake
    {
        get => (System.String)this[nameof(Handshake)];
        set => this[nameof(Handshake)] = value;
    }
	[UserScopedSetting()]
    [DefaultSettingValue("ASCII")]
    public System.String Format
    {
        get => (System.String)this[nameof(Format)];
        set => this[nameof(Format)] = value;
    }
	[UserScopedSetting()]
    [DefaultSettingValue("500")]
    public System.Int32 BytesPerLine
    {
        get => (System.Int32)this[nameof(BytesPerLine)];
        set => this[nameof(BytesPerLine)] = value;
    }
}

