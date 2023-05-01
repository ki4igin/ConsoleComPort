using AutoCompleteConsole;
using System.Drawing;

namespace ConsoleComPort;

public static class Info
{
    public static void Print(string str) =>
        Acc.WriteFirstLine(str);
    
    public static void PrintSendMessage(string str) =>
        Acc.WriteFirstLine($"{str}", EscColor.ForegroundDarkYellow);
    public static void PrintInfo(string str) =>
        Print(str, "Info", EscColor.ForegroundDarkGreen);

    public static void PrintWarning(string str) =>
        Print(str, "Warning", EscColor.ForegroundDarkYellow);
    
    public static void PrintError(string str) =>
        Print(str, "Error", EscColor.ForegroundDarkRed);

    private static void Print(string str, string type, EscColor color)
    {
        string typeStr = $"{type}: ".Color(color);
        Acc.WriteFirstLine($"{typeStr}" + str.Replace("\n", $"\n{typeStr}"));
    }
}