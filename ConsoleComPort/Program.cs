using System;
using System.Collections.Generic;
using System.Linq;
using AutoCompleteConsole;

namespace ConsoleComPort
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine();
            
            AppSettings appSettings = AppSettings.Load();
            ComPort comPort = new(appSettings);

            // Saving settings after exiting the program
            AppDomain.CurrentDomain.ProcessExit += (_, _) => AppSettings.Save(appSettings);

            Dictionary<string, Action> commands = new()
            {
                ["start monitor"] = comPort.ReceiveStart,
                ["stop monitor"] = comPort.ReceiveStop,
                ["reboot"] = comPort.ReceiveReboot,
                ["settings set all"] = appSettings.SetAllSettings,
                ["settings set baudrate"] = appSettings.SetBaudRate,
                ["settings set portname"] = appSettings.SetPortName,
                ["settings set party"] = appSettings.SetParty,
                ["settings set stopbits"] = appSettings.SetStopBits,
                ["settings set bytes per line"] = appSettings.SetBytesPerLine,
                ["display settings"] = comPort.DisplaySettings,
                ["save settings"] = () => AppSettings.Save(appSettings),
            };

            Acc.AddKeyWords(commands.Keys.ToArray());

            while (true)
            {
                string command = Acc.ReadLine();
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
