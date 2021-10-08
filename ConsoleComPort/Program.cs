using AppTools;
using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleComPort.AppTools;

namespace ConsoleComPort
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine();
            
            ComPort comPort = new();

            // Saving settings after exiting the program
            AppDomain.CurrentDomain.ProcessExit += (_, _) => comPort.SaveSetting();

            Dictionary<string, Action> commands = new()
            {
                ["start monitor"] = comPort.ReceiveStart,
                ["stop monitor"] = comPort.ReceiveStop,
                ["reboot"] = comPort.ReceiveReboot,
                ["clear"] = Console.Clear,
                ["settings"] = comPort.SetAllSettings,
                ["quit program"] = null,
                ["display settings"] = comPort.DisplaySettings,
                ["save settings"] = comPort.SaveSetting,
                ["save settings to file"] = comPort.SaveSettingToFile,
                ["read settings from file"] = comPort.ReadSettingFromFile
            };

            MyConsole.SetCmdDictionary(commands.Keys.ToArray());

            while (true)
            {
                string command = MyConsole.ReadLine();
                string str = command;
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
                    comPort.Transmit(str);
                }
            }
        }
    }
}
