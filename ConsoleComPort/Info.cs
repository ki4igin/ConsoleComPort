using AutoCompleteConsole;

namespace ConsoleComPort;

public static class Info
{
    public static void Print(string str) =>
        Acc.WriteFirstLine(str);
    
    public static void PrintSendMessage(string str) =>
        Acc.WriteFirstLine($"{str}", EscColor.ForegroundDarkYellow);
    public static void PrintInfo(string str) =>
        Acc.WriteFirstLine("Info: ".Color(EscColor.ForegroundDarkGreen) + str);

    public static void PrintWarning(string str) =>
        Acc.WriteFirstLine("Warning: ".Color(EscColor.ForegroundDarkYellow) + str);
    
    public static void PrintError(string str) =>
        Acc.WriteFirstLine("Error: ".Color(EscColor.ForegroundDarkRed) + str);
}