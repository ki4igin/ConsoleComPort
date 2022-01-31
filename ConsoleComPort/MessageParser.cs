using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleComPort.AppTools;

namespace ConsoleComPort
{
    public static class MessageParser
    {
        public static byte[] ParseMessage(string message)
        {
            byte[] sendBytes;
            List<string> listSendBits = new();
            message = Regex.Replace(message, @"[_,-]", "");

            if (Regex.IsMatch(message, @"^[0-9]{1,3}[x,b,d]"))
            {
                var words = message.Split(" ");
                foreach (var word in words)
                {
                    var bitValue = string.Empty;
                    if (Regex.IsMatch(word, @"^[0-9]{0,3}[x][0-F]*$", RegexOptions.IgnoreCase))
                    {
                        bitValue = ConvertWordToBitString(word[0] == 'x' ? $"0{word}" : word, 'x');
                    }
                    else if (Regex.IsMatch(word, @"^[0-9]{0,3}[b][0,1]*$", RegexOptions.IgnoreCase))
                    {
                        bitValue = ConvertWordToBitString(word[0] == 'b' ? $"0{word}" : word, 'b');
                    }
                    else if (Regex.IsMatch(word, @"^[0-9]{0,3}[d][0-9]*$", RegexOptions.IgnoreCase))
                    {
                        bitValue = ConvertWordToBitString(word[0] == 'd' ? $"0{word}" : word, 'd');
                    }
                    else if (Regex.IsMatch(word, @"^[0-9]*$", RegexOptions.IgnoreCase))
                    {
                        bitValue = ConvertWordToBitString($"0d{word}", 'd');
                    }
                    else
                    {
                        MyConsole.WriteLineYellow($"Warning: word \"{word}\" has unknown format");
                    }

                    if (bitValue != string.Empty)
                    {
                        listSendBits.Add(bitValue);
                    }
                }

                var sendStr = string.Join("", listSendBits);


                var numberAddZeros = (8 - sendStr.Length % 8) % 8;
                if (numberAddZeros != 0)
                {
                    sendStr = new string('0', numberAddZeros) + sendStr;
                    MyConsole.WriteLineYellow($"Warning: Added {numberAddZeros} zeros");
                }

                var numBytes = sendStr.Length / 8;
                sendBytes = new byte[numBytes];
                for (var i = 0; i < numBytes; i++)
                {
                    sendBytes[i] = Convert.ToByte(sendStr.Substring(8 * i, 8), 2);
                }
            }
            else
            {
                sendBytes = Encoding.ASCII.GetBytes(message);
            }

            return sendBytes;
        }

        private static string ConvertWordToBitString(string word, char separator)
        {
            var numberBitsAndValue = word.Split(separator, 2);
            var numberBits = int.Parse(numberBitsAndValue[0]);
            var value = numberBitsAndValue[1].ToUpper();

            var bitValue = value;

            bitValue = separator switch
            {
                'b' => bitValue,
                'x' => ConvertStringNumberToBinary(bitValue, 16),
                'd' => ConvertStringNumberToBinary(bitValue, 10),
                _ => throw new ArgumentOutOfRangeException(nameof(separator), separator, null)
            };

            var deltaNumBits = numberBits - bitValue.Length;
            var numberAddZeros = (8 - bitValue.Length % 8) % 8;
            bitValue = numberBits switch
            {
                0 => new string('0', numberAddZeros) + bitValue,
                _ => deltaNumBits switch
                {
                    > 0 => new string('0', deltaNumBits) + bitValue,
                    < 0 => bitValue.Remove(0, -deltaNumBits),
                    _ => bitValue
                }
            };

            return bitValue;
        }

        private static string ConvertStringNumberToBinary(string str, int fromBase)
        {
            switch (fromBase)
            {
                case 16:
                    return string.Join("", str.Select(DecodingHexDigitToBin));
                case 10 when uint.TryParse(str, out uint temp):
                    return Convert.ToString(temp, 2);
                case 10:
                    MyConsole.WriteLineYellow($"Warning:Unsigned number \"{str}\" is too large");
                    return string.Empty;
                default:
                    MyConsole.WriteLineRed("Error to method ConvertStringNumberToBinary");
                    return string.Empty;
            }
        }

        private static string DecodingHexDigitToBin(char hex) =>
            hex switch
            {
                '0' => "0000",
                '1' => "0001",
                '2' => "0010",
                '3' => "0011",
                '4' => "0100",
                '5' => "0101",
                '6' => "0110",
                '7' => "0111",
                '8' => "1000",
                '9' => "1001",
                'A' => "1010",
                'B' => "1011",
                'C' => "1100",
                'D' => "1101",
                'E' => "1110",
                'F' => "1111",
                _ => throw new ArgumentOutOfRangeException(nameof(hex), hex, null)
            };
    }
}