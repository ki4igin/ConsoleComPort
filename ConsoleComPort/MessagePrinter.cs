using AutoCompleteConsole;

namespace ConsoleComPort;

public static class MessagePrinter
{
    public static void PrintWarning(string str) =>
        Acc.WriteLine($"Warning: {str}", EscColor.ForegroundYellow);
    
    public static void PrintError(string str) =>
        Acc.WriteLine($"Error: {str}", EscColor.ForegroundRed);
}