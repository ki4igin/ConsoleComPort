using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static System.BitConverter;

namespace ConsoleComPort;

public partial class ReceiveMessageParser
{
    private enum TypeView
    {
        Bin,
        Decimal,
        Hex,
        Char
    }

    private enum Type
    {
        U256,
        U128,
        U64,
        U32,
        U16,
        U8,
        I256,
        I128,
        I64,
        I32,
        I16,
        I8,
        F64,
        F32,
        Char
    }

    private record struct Format(int Count, TypeView TypeView, Type Type, int Size);

    private static readonly Regex _regexNumber = RegexNumber();
    private static readonly Regex _regexChar = RegexChar();
    
    private readonly List<Format> _formats;
    private bool _isStringFormat;

    public int BytesCount { get; private set; }

    public ReceiveMessageParser(string formatString)
    {
        _formats = new();
        _isStringFormat = true;
        BytesCount = 0;

        ChangeFormat(formatString);
    }

    public string Parse(byte[] bytes)
    {
        if (bytes.Length != BytesCount && BytesCount != 0)
            return "";

        if (_isStringFormat)
            return Encoding.ASCII.GetString(bytes);

        string[] words = new string[_formats.Count];
        int indexStart = 0;
        for (int i = 0; i < _formats.Count; i++)
        {
            int size = _formats[i].Size;
            words[i] = ParseWord(bytes[indexStart..(indexStart + size)], _formats[i]);
            indexStart += size;
        }

        return string.Join(" ", words) + Environment.NewLine;
    }

    public bool ChangeFormat(string formatString)
    {
        _formats.Clear();
        BytesCount = 0;

        if (formatString is "str" or "")
        {
            _isStringFormat = true;
            return true;
        }

        _isStringFormat = false;

        foreach (string formatWord in formatString.Trim().Split())
        {
            Match matchChar = _regexChar.Match(formatWord);
            Match matchNumber = _regexNumber.Match(formatWord);

            Format format;

            if (matchChar.Success)
                format = ParseChar(matchChar);
            else if (matchNumber.Success)
                format = ParseNumber(matchNumber);
            else
                continue;

            _formats.Add(format);
            BytesCount += format.Size;
        }

        return _formats.Count > 0;
    }

    public static string ValidateFormat(string formatString)
    {
        string[] formats = formatString.Trim().Split();
        if (formatString is "str" or "")
            return "";
        
        string[] errors = new string[formats.Length];
        bool errorsFound = false;
        for (int i = 0; i < formats.Length; i++)
        {
            Match matchChar = _regexChar.Match(formats[i]);
            Match matchNumber = _regexNumber.Match(formats[i]);
            bool isError = matchChar.Success == false && matchNumber.Success == false;
            if (isError)
                errorsFound = true;
            errors[i] = isError switch
            {
                true => new('~', formats[i].Length),
                _ => new(' ', formats[i].Length)
            };
        }

        return errorsFound switch
        {
            true => string.Join(" ", errors) + " Wrong format",
            false => ""
        };
    }

    private static string ParseWord(byte[] bytes, Format format)
    {
        (int count, TypeView typeView, Type type, _) = format;
        int size = GetTypeSize(type);

        if (type == Type.Char)
            return Encoding.ASCII.GetString(bytes);

        string[] words = new string[count];

        for (int i = 0; i < count; i++)
        {
            byte[] wordBytes = bytes[(i * size)..((i + 1) * size)];

            words[i] = (typeView, type) switch
            {
                (TypeView.Decimal, Type.U64) => $"{ToUInt64(wordBytes)}".PadLeft(20),
                (TypeView.Decimal, Type.U32) => $"{ToUInt32(wordBytes)}".PadLeft(10),
                (TypeView.Decimal, Type.U16) => $"{ToUInt16(wordBytes)}".PadLeft(5),
                (TypeView.Decimal, Type.U8) => $"{wordBytes[0]}".PadLeft(3),
                (TypeView.Decimal, Type.I64) => $"{ToInt64(wordBytes)}".PadLeft(20),
                (TypeView.Decimal, Type.I32) => $"{ToInt32(wordBytes)}".PadLeft(11),
                (TypeView.Decimal, Type.I16) => $"{ToInt16(wordBytes)}".PadLeft(6),
                (TypeView.Decimal, Type.I8) => $"{(char) wordBytes[0]}".PadLeft(4),
                (TypeView.Decimal, Type.F64) => $"{ToDouble(wordBytes)}",
                (TypeView.Decimal, Type.F32) => $"{ToSingle(wordBytes)}",
                (TypeView.Bin, Type.U64 or Type.I64) => $"0b{Convert.ToString(ToInt64(wordBytes), 2).PadLeft(64, '0')}",
                (TypeView.Bin, Type.U32 or Type.I32) => $"0b{Convert.ToString(ToInt32(wordBytes), 2).PadLeft(32, '0')}",
                (TypeView.Bin, Type.U16 or Type.I16) => $"0b{Convert.ToString(ToInt16(wordBytes), 2).PadLeft(16, '0')}",
                (TypeView.Bin, Type.U8 or Type.I8) => $"0b{Convert.ToString(wordBytes[0], 2).PadLeft(8, '0')}",
                (TypeView.Hex, Type.U64 or Type.I64) => $"0x{ToInt64(wordBytes):X16}",
                (TypeView.Hex, Type.U32 or Type.I32) => $"0x{ToInt32(wordBytes):X8}",
                (TypeView.Hex, Type.U16 or Type.I16) => $"0x{ToInt16(wordBytes):X4}",
                (TypeView.Hex, Type.U8 or Type.I8) => $"0x{wordBytes[0]:X2}",
                // (TypeView.Bin, Type.F64) => $"0b{Convert.ToString(BitConverter.ToDouble(wordBytes), 2)}",
                // (TypeView.Bin, Type.F32) => $"0b{Convert.ToString(BitConverter.ToSingle(wordBytes), 2)}",
                _ => throw new ArgumentOutOfRangeException($"{nameof(typeView)} {nameof(type)}", (typeView, type), null)
            };
        }

        return string.Join(" ", words);
    }

    private static Format ParseChar(Match match)
    {
        int count = ParseCount(match.Groups[1].Value);
        int size = count * GetTypeSize(Type.Char);
        return new(count, TypeView.Char, Type.Char, size);
    }

    private static Format ParseNumber(Match match)
    {
        int count = ParseCount(match.Groups[1].Value);
        TypeView typeView = match.Groups[2].Value switch
        {
            "x" => TypeView.Hex,
            "b" => TypeView.Bin,
            _ => TypeView.Decimal
        };
        Type type = match.Groups[3].Value switch
        {
            "u256" => Type.U256,
            "u128" => Type.U128,
            "u64" => Type.U64,
            "u32" => Type.U32,
            "u16" => Type.U16,
            "u8" => Type.U8,
            "i256" => Type.I256,
            "i128" => Type.I128,
            "i64" => Type.I64,
            "i32" => Type.I32,
            "i16" => Type.I16,
            "i8" => Type.I8,
            "f64" => Type.F32,
            "f32" => Type.F64,
            _ => Type.Char,
        };
        int size = count * GetTypeSize(type);
        return new(count, typeView, type, size);
    }

    private static int GetTypeSize(Type type) =>
        type switch
        {
            Type.I256 or Type.U256 => 32,
            Type.I128 or Type.U128 => 16,
            Type.I64 or Type.U64 or Type.F64 => 8,
            Type.I32 or Type.U32 or Type.F32 => 4,
            Type.I16 or Type.U16 => 2,
            Type.I8 or Type.U8 or Type.Char => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    private static int ParseCount(string str) =>
        str switch
        {
            "" or "0" or "1" => 1,
            _ => int.Parse(str)
        };

    [GeneratedRegex(@"(\d*)(x|b?)(i8|i16|i32|i64|i128|i256|u8|u16|u32|u64|u128|u256|f32|f64)(?!.)")]
    private static partial Regex RegexNumber();

    [GeneratedRegex(@"(\d*)(ch|char)(?!.)")]
    private static partial Regex RegexChar();
}