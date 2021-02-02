using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleComPort
{
    static class MyConsole
    {
        static private string[] _cmdKeyWords = new string[1] { "" };
        private static readonly object _lockWrtie = new();
        private static readonly List<string> _cmdHistory = new List<string>();

        private static int _posCursorCmd = 0;
        private static int _posCursorData = 0;

        static string _cmdStr;
        static string CmdStr
        {
            get => _cmdStr;
            set
            {
                _cmdStr = value;
                var emptyStr = $"\r{ new string(' ', Console.WindowWidth - 1) }\r";
                lock (_lockWrtie)
                {
                    Console.CursorVisible = false;
                    Console.Write(emptyStr);
                    Console.Write(value);
                    Console.CursorLeft = _posCursorCmd;
                    Console.CursorVisible = true;
                }
            }
        }

        public static void SetCmdDictionary(string[] keyWords)
        {
            _cmdKeyWords = keyWords;
        }

        public static void WriteNewLineGreen(string str) => WriteNewLine(str, ConsoleColor.Green);
        public static void WriteNewLineRed(string str) => WriteNewLine(str, ConsoleColor.Red);
        public static void WriteNewLineYellow(string str) => WriteNewLine(str, ConsoleColor.DarkYellow);
        public static void WriteLineGreen(string str) => WriteLine(str, ConsoleColor.Green);
        public static void WriteLineRed(string str) => WriteLine(str, ConsoleColor.Red);
        public static void WriteLineYellow(string str) => WriteLine(str, ConsoleColor.DarkYellow);

        public static void WriteNewLine(string str, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            WriteNewLine(str);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void WriteNewLine(string str = "")
        {
            var tempStr = _posCursorData == 0 ? $"{str}\r\n" : $"\r\n{str}\r\n";
            Write(tempStr);
        }
        public static void WriteLine(string str, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void WriteLine(string str = "")
        {
            var tempStr = str + "\r\n";
            Write(tempStr);
        }
        public static void Write(string str = "")
        {
            if (Console.CursorTop < 2) Console.CursorTop = 2;
            var emptyStr = $"\r{ new string(' ', Console.WindowWidth - 1) }\r";
            lock (_lockWrtie)
            {
                Console.CursorVisible = false;
                Console.Write(emptyStr);
                Console.SetCursorPosition(_posCursorData, Console.CursorTop - 2);
                Console.Write(str);
                _posCursorData = Console.CursorLeft;
                Console.Write("\r\n\n"); // SetCursorPosition(0, Console.CursorTop + 2);               
                Console.Write(CmdStr);
                Console.CursorLeft = _posCursorCmd;
                Console.CursorVisible = true;
            }
        }

        public static string ReadLine()
        {
            StringBuilder sb = new StringBuilder();
            List<string> cmdTabList = new List<string>();

            int tabCnt = 0;
            int cmdIndex = _cmdHistory.Count;
            int posCursorCmd = 0;

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }
                if (keyInfo.Key != ConsoleKey.Tab)
                {
                    tabCnt = 0;
                }
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Backspace:
                        if (posCursorCmd != 0)
                        {
                            posCursorCmd--;
                            sb.Remove(posCursorCmd, 1);
                        }
                        break;
                    case ConsoleKey.Delete:
                        if (posCursorCmd != sb.Length)
                        {
                            sb.Remove(posCursorCmd, 1);
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (posCursorCmd != 0)
                        {
                            posCursorCmd--;
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (posCursorCmd != sb.Length)
                        {
                            posCursorCmd++;
                        }
                        break;
                    case ConsoleKey.UpArrow:
                        if (cmdIndex != 0)
                        {
                            cmdIndex--;
                            sb.Clear();
                            sb.Append(_cmdHistory[cmdIndex]);
                            posCursorCmd = sb.Length;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        cmdIndex++;
                        if (cmdIndex < _cmdHistory.Count)
                        {
                            sb.Clear();
                            sb.Append(_cmdHistory[cmdIndex]);
                            posCursorCmd = sb.Length;
                        }
                        else
                        {
                            cmdIndex--;
                        }
                        break;
                    case ConsoleKey.End:
                        posCursorCmd = sb.Length;
                        break;
                    case ConsoleKey.Home:
                        posCursorCmd = 0;
                        break;
                    case ConsoleKey.Tab:
                        if (tabCnt == 0)
                        {
                            cmdTabList.Clear();
                            foreach (var word in _cmdKeyWords)
                            {
                                if (word.StartsWith(sb.ToString()))
                                {
                                    cmdTabList.Add(word);
                                }
                            }
                        }
                        tabCnt++;
                        if (cmdTabList.Count > 0)
                        {
                            int cnt = (tabCnt - 1) % cmdTabList.Count;
                            sb.Clear();
                            sb.Append(cmdTabList[cnt]);
                            posCursorCmd = sb.Length;
                        }
                        break;
                    case ConsoleKey.Enter:
                        break;
                    default:
                        if (keyInfo.Key >= ConsoleKey.D0 && keyInfo.Key <= ConsoleKey.Z ||
                            keyInfo.Key >= ConsoleKey.NumPad0 && keyInfo.Key <= ConsoleKey.Divide ||
                            keyInfo.Key >= ConsoleKey.Oem1 && keyInfo.Key <= ConsoleKey.Oem102 ||
                            keyInfo.Key == ConsoleKey.Spacebar)
                        {
                            sb.Insert(posCursorCmd, keyInfo.KeyChar);
                            posCursorCmd++;
                        }
                        break;
                }
                _posCursorCmd = posCursorCmd;
                CmdStr = sb.ToString();
            }
            _posCursorCmd = 0;
            _cmdHistory.Add(sb.ToString());
            var resultStr = CmdStr;
            CmdStr = string.Empty;
            return resultStr;
        }

        private static void ConsoleWriteLineGreen(string str) => ConsoleWriteLine(str, ConsoleColor.Green);
        private static void ConsoleWriteLineRed(string str) => ConsoleWriteLine(str, ConsoleColor.Red);
        private static void ConsoleWriteLine(string str, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
        }

        const int ALLIGMEND = 6;
        const int START_LIST_NUMBER = 1;

        public static string SelectFromList(
            string[] settings,
            string nameSetting = "Parametrs",
            int defaultSettingNum = 0,
            string comment = "",
            bool enableAlligmend = false)
        {
            int strPos = 0;
            lock (_lockWrtie)
            {
                Console.CursorVisible = false;
                Console.Clear();
                defaultSettingNum = defaultSettingNum >= 0 ? defaultSettingNum : 0;

                ConsoleWriteLineGreen($"Available {nameSetting}:");
                Console.WriteLine(comment);
                int cursorTopInit = Console.CursorTop;
                foreach (string setting in settings)
                {
                    var str = $"{(enableAlligmend ? $"{setting,ALLIGMEND}" : $"{setting}")}";
                    Console.WriteLine($"[{strPos++ + START_LIST_NUMBER}] {str}");
                }

                strPos = defaultSettingNum;
                Console.CursorTop = cursorTopInit + strPos;

                ConsoleKey key;
                int oldStrPos = strPos;
                do
                {
                    HighlightStr(settings, oldStrPos, strPos, enableAlligmend);
                    oldStrPos = strPos;
                    key = Console.ReadKey(true).Key;
                    strPos = key switch
                    {
                        ConsoleKey.UpArrow => (oldStrPos == 0) ? (settings.Length - 1) : (oldStrPos - 1),
                        ConsoleKey.DownArrow => (oldStrPos == settings.Length - 1) ? (0) : (oldStrPos + 1),
                        ConsoleKey.Escape => defaultSettingNum,
                        >= ConsoleKey.D0 and <= ConsoleKey.D9 when
                            key - ConsoleKey.D0 >= START_LIST_NUMBER &&
                            key - ConsoleKey.D0 <= START_LIST_NUMBER + settings.Length - 1 =>
                            key - ConsoleKey.D0 - START_LIST_NUMBER,
                        >= ConsoleKey.NumPad0 and <= ConsoleKey.NumPad9 when
                            key - ConsoleKey.NumPad0 >= START_LIST_NUMBER &&
                            key - ConsoleKey.NumPad0 <= START_LIST_NUMBER + settings.Length - 1 =>
                            key - ConsoleKey.NumPad0 - START_LIST_NUMBER,
                        _ => oldStrPos
                    };

                } while (key is not ConsoleKey.Enter and not ConsoleKey.Escape);
                Console.Clear();
            }
            return settings[strPos];
        }

        private static void HighlightStr(string[] vs, int strPosOld, int strPosNew, bool enableAlligmend = false)
        {
            var str1 = $"{(enableAlligmend ? $"{vs[strPosOld],ALLIGMEND}" : $"{vs[strPosOld]}")}";

            Console.Write($"[{strPosOld + START_LIST_NUMBER}] {str1}\r");
            Console.CursorTop += (strPosNew - strPosOld);
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            var str2 = $"{(enableAlligmend ? $"{vs[strPosNew],ALLIGMEND}" : $"{vs[strPosNew]}")}";
            Console.Write($"[{strPosNew + START_LIST_NUMBER}] {str2}\r");
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public static int ReadNumber(
            string nameSetting = "Parametrs",
            int defaultValue = 0,
            Predicate<int> validator = default)
        {
            int result = defaultValue;
            lock (_lockWrtie)
            {

                if (validator == null)
                {
                    validator = (p) => true;
                }

                Console.CursorVisible = true;
                Console.Clear();
                ConsoleWriteLineGreen($"Введите {nameSetting} (текущее значение {defaultValue}):");
                Console.WriteLine();

                int maxErrorCnt = 3;
                while (maxErrorCnt > 0)
                {
                    string str = ReadLineCancel();
                    if (int.TryParse(str, out int temp) && validator(result))
                    {
                        result = temp;
                        break;
                    }
                    else if (str == "")
                    {
                        break;
                    }
                    else
                    {
                        ConsoleWriteLineRed("Введите КОРРЕКТНОЕ значение:");
                        maxErrorCnt--;
                    }
                }
                Console.Clear();
            }
            return result;
        }

        static string ReadLineCancel()
        {
            StringBuilder sb = new StringBuilder();
            int posCursorCmd = 0;

            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }
                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    return "";
                }
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Backspace:
                        if (posCursorCmd != 0)
                        {
                            posCursorCmd--;
                            sb.Remove(posCursorCmd, 1);
                        }
                        break;
                    case ConsoleKey.Delete:
                        if (posCursorCmd != sb.Length)
                        {
                            sb.Remove(posCursorCmd, 1);
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (posCursorCmd != 0)
                        {
                            posCursorCmd--;
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (posCursorCmd != sb.Length)
                        {
                            posCursorCmd++;
                        }
                        break;
                    case ConsoleKey.End:
                        posCursorCmd = sb.Length;
                        break;
                    case ConsoleKey.Home:
                        posCursorCmd = 0;
                        break;
                    case ConsoleKey.Enter:
                        break;
                    default:
                        if (keyInfo.Key >= ConsoleKey.D0 && keyInfo.Key <= ConsoleKey.D9 ||
                            keyInfo.Key >= ConsoleKey.NumPad0 && keyInfo.Key <= ConsoleKey.NumPad9)
                        {
                            sb.Insert(posCursorCmd, keyInfo.KeyChar);
                            posCursorCmd++;
                        }
                        break;
                }
                Console.CursorVisible = false;
                var emptyStr = $"\r{ new string(' ', Console.WindowWidth - 1) }\r";
                Console.Write(emptyStr);
                Console.Write(sb.ToString());
                Console.CursorLeft = posCursorCmd;
                Console.CursorVisible = true;
            }
            Console.CursorLeft = 0;
            return sb.ToString();
        }
    }
}
