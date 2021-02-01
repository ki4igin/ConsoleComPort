using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleComPort
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine();
            ComPort comPort = new ComPort();

            /// Сохрание настроек после выхода из программы
            AppDomain.CurrentDomain.ProcessExit += new EventHandler((obj, ev) => comPort.SaveSetting());


            Dictionary<string, Action> commands = new()
            {
                ["start"] = comPort.ReceiveStart,
                ["stop"] = comPort.ReceiveStop,
                ["reboot"] = comPort.ReceiveReboot,
                ["clear"] = Console.Clear,
                ["settings"] = comPort.SetAllSettings,
                ["quit"] = null,
                ["display settings"] = comPort.DisplaySettings,
                ["save settings"] = comPort.SaveSetting,
                ["save settings to file"] = comPort.SaveSettingToFile,
                ["read settings from file"] = comPort.ReadSettingFromFile
            };

            MyConsole.SetCmdDictionary(commands.Keys.ToArray());

            while (true)
            {
                string command = MyConsole.ReadLine();
                command = command.ToLower();
                if (commands.TryGetValue(command, out Action executeCmd))
                {
                    if (executeCmd == null)
                    {
                        break;
                    }
                    executeCmd();
                }
                else
                {
                    comPort.Transmit(command);
                }
            }
        }
    }
}
