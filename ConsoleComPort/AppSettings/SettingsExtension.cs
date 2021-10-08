using AppTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ConsoleComPort.AppTools;

namespace AppSettings
{
    public interface ISettings
    {

    }

    public static class SettingsExtension
    {
        public static void Display<T>(this T settings) where T : ISettings, IDescription
        {
            MyConsole.WriteNewLineGreen($"Current Settings");
            foreach (var setting in settings.GetType().GetProperties())
            {
                var description = settings.GetPropertyDescription(setting.Name);
                var value = setting.GetValue(settings);
                MyConsole.WriteLine($"{description,-40} {value}");
            }
        }

        public static void Save<T>(this T settings) where T : ISettings, IDescription
        {
            var dirPath = AppContext.BaseDirectory + "/settings";
            var fileName = ".settings";
            var filePath = $"{dirPath}/{fileName}.json";
            Directory.CreateDirectory(dirPath);
            string jsonString = settings.SaveToStr();
            File.WriteAllText(filePath, jsonString);
        }
        public static void Read<T>(this T settings) where T : ISettings, IDescription
        {
            var dirPath = AppContext.BaseDirectory + "/settings";
            var fileName = ".settings";
            var filePath = $"{dirPath}/{fileName}.json";
            if (File.Exists(filePath) is false)
                return;
            var jsonString = File.ReadAllText(filePath);
            settings.ReadFromStr(jsonString);
        }
        public static void SaveToFile<T>(this T settings) where T : ISettings
        {
            MyConsole.WriteNewLineGreen("Введите имя файла для сохранения настроек");
            var fileName = MyConsole.ReadLine();
            var dirPath = AppContext.BaseDirectory + "/settings";
            var filePath = $"{dirPath}/{fileName}.json";
            Directory.CreateDirectory(dirPath);
            string jsonString = settings.SaveToStr();
            File.WriteAllText(filePath, jsonString);
            MyConsole.WriteNewLineGreen($"Настройки сохранены в файл {fileName}.json");
        }
        public static void ReadFromFile<T>(this T settings) where T : ISettings
        {
            var dirPath = AppContext.BaseDirectory + "/settings";
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            List<string> fileNames = new List<string>();
            foreach (var file in dir.GetFiles("*.json"))
            {
                fileNames.Add(Path.GetFileNameWithoutExtension(file.Name));
            }
            if (fileNames.Count == 0)
            {
                MyConsole.WriteNewLineRed("Файлы настроек не найдены!");
                return;
            }
            var fileName = MyConsole.SelectFromList(fileNames.ToArray(), "Файлы");
            var filePath = $"{dirPath}/{fileName}.json";
            var jsonString = File.ReadAllText(filePath);
            settings.ReadFromStr(jsonString);       
        }

        private static string SaveToStr<T>(this T settings) where T : ISettings
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(settings, jsonOptions);
        }
        private static void ReadFromStr<T>(this T settings, string str) where T : ISettings
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var newSettings = JsonSerializer.Deserialize<T>(str, jsonOptions);
            CopyAllProperty(newSettings, settings);
        }
        private static void CopyAllProperty<T>(T source, T target)
        {
            var type = typeof(T);
            foreach (var sourceProperty in type.GetProperties())
            {
                var targetProperty = type.GetProperty(sourceProperty.Name);
                targetProperty.SetValue(target, sourceProperty.GetValue(source, null), null);
            }
        }
    }
}
