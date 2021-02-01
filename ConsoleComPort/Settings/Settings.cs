using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConsoleComPort
{
    [Serializable]
    public class Settings
    {
        public struct Setting<T>
        {
            public string Description;
            public T Value;
        }

        public Setting<string> PortName = new() { Description = "Port", Value = "COM1" };
        public Setting<int> BaudRate = new() { Description = "BaudRate", Value = 9600 };
        public Setting<string> Parity = new() { Description = "Parity", Value = "None" };
        public Setting<int> DataBits = new() { Description = "DataBits", Value = 8 };
        public Setting<string> StopBits = new() { Description = "StopBits", Value = "One" };
        public Setting<string> Handshake = new() { Description = "Handshake", Value = "None" };
        public Setting<string> Format = new() { Description = "Format Receive", Value = "ASCII" };
        public Setting<int> BytesPerLine = new() { Description = "Bytes per line", Value = 500 };


        public Settings()
        {

        }

        public void Save()
        {
            var settings = this;
            var settingsDictionary = new Dictionary<string, object>();
            foreach (var fields in settings.GetType().GetFields())
            {
                var setting = fields.GetValue(settings);
                var name = fields.Name;
                var value = setting
                    .GetType()
                    .GetField(nameof(Settings.Setting<string>.Value))
                    .GetValue(setting);
                settingsDictionary.Add(name, value);
            }

            var globalSettings = new GlobalSettings();
            foreach (var prop in globalSettings.GetType().GetProperties())
            {
                if (settingsDictionary.TryGetValue(prop.Name, out object value))
                {
                    prop.SetValue(globalSettings, value);
                }
            }
            globalSettings.Save();
        }

        public void Read()
        {
            var globalSettings = new GlobalSettings();
            var settingsDictionary = new Dictionary<string, System.Reflection.PropertyInfo>();
            foreach (var prop in globalSettings.GetType().GetProperties())
            {
                var name = prop.Name;
                settingsDictionary.Add(name, prop);
            }
            var settings = this;
            foreach (var fields in settings.GetType().GetFields())
            {
                var setting = fields.GetValue(settings);
                if (settingsDictionary.TryGetValue(fields.Name, out System.Reflection.PropertyInfo value))
                {
                    setting
                        .GetType()
                        .GetField(nameof(Settings.Setting<string>.Value))
                        .SetValue(setting, value.GetValue(globalSettings));
                }
                fields.SetValue(settings, setting);
            }
        }


        public static void SaveToFile<T>(T settings)
        {
            MyConsole.WriteNewLineGreen("Введите имя файла для сохранения настроек");
            var fileName = MyConsole.ReadLine() + ".json";
            var filePath = $"./settings/{fileName}";
            Directory.CreateDirectory("settings");
            var jsonOptions = new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string jsonString = JsonSerializer.Serialize(settings, jsonOptions);
            File.WriteAllText(filePath, jsonString);
            MyConsole.WriteNewLineGreen($"Настройки сохранены в файл {fileName}");
        }

        public static T ReadFromFile<T>()
        {
            var path = "./settings";
            DirectoryInfo dir = new DirectoryInfo(path);
            List<string> fileNames = new List<string>();
            foreach (var file in dir.GetFiles("*.json"))
            {
                fileNames.Add(Path.GetFileNameWithoutExtension(file.Name));
            }
            if (fileNames.Count == 0)
            {
                MyConsole.WriteNewLineRed("Файлы настроек не найдены!");
            }

            var fileName = MyConsole.SelectFromList(fileNames.ToArray(), "Файлы");
            var filePath = $"{path}/{fileName}.json";
            var jsonOptions = new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(jsonString, jsonOptions);

        }

        public static void Display<T>(T settings)
        {
            MyConsole.WriteNewLineGreen($"Current Settings");
            foreach (var fields in settings.GetType().GetFields())
            {
                var setting = fields.GetValue(settings);
                var description = setting
                    .GetType()
                    .GetField(nameof(Setting<string>.Description))
                    .GetValue(setting)
                    .ToString();
                var value = setting
                    .GetType()
                    .GetField(nameof(Setting<string>.Value))
                    .GetValue(setting)
                    .ToString();
                MyConsole.WriteLine($"{description,-20} {value}");
            }
            MyConsole.WriteLine();
        }

        private static void CopyAllFiledsAndProperty<T>(T source, T target)
        {
            var type = typeof(T);
            foreach (var sourceProperty in type.GetProperties())
            {
                var targetProperty = type.GetProperty(sourceProperty.Name);
                targetProperty.SetValue(target, sourceProperty.GetValue(source, null), null);
            }
            foreach (var sourceField in type.GetFields())
            {
                var targetField = type.GetField(sourceField.Name);
                targetField.SetValue(target, sourceField.GetValue(source));
            }
        }

    }

}






