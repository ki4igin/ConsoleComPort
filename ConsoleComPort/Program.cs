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
            AppSettings appSettings = AppSettings.Load();
            ReceiveMessageParser receiveMessageParser = new(appSettings.Format);
            ComPort comPort = new(appSettings, receiveMessageParser);

            // Saving settings after exiting the program
            AppDomain.CurrentDomain.ProcessExit += (_, _) => AppSettings.Save(appSettings);
            Console.CancelKeyPress += (_, _) => AppSettings.Save(appSettings);

            Dictionary<string, Action?> commands = new()
            {
                ["open"] = comPort.Open,
                ["close"] = comPort.Close,
                ["reopen"] = comPort.ReOpen,
                ["settings all"] = appSettings.SetAllSettings,
                ["settings baudrate"] = appSettings.SetBaudRate,
                ["settings portname"] = appSettings.SetPortName,
                ["settings party"] = appSettings.SetParty,
                ["settings stopbits"] = appSettings.SetStopBits,
                ["settings format"] = appSettings.SetFormat,
                ["settings display"] = comPort.DisplaySettings,
                ["settings save"] = () => AppSettings.Save(appSettings),
            };

            Acc.AddKeyWords(commands.Keys.ToArray());

            while (true)
            {

                string command = Acc.ReadLine();
                string str = command;
                command = command.ToLower();
                if (commands.TryGetValue(command, out Action? executeCmd))
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
