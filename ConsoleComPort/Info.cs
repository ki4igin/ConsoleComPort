using AutoCompleteConsole;

namespace ConsoleComPort;

public static class Info
{
    public static void PrintWarning(string str) =>
        Acc.WriteLine($"Warning: {str}", EscColor.ForegroundDarkYellow);
    
    public static void PrintError(string str) =>
        Acc.WriteLine($"Error: {str}", EscColor.ForegroundRed);
}