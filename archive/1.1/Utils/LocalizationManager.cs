using System.Collections.Generic;
using RunCrypt.Models;

namespace RunCrypt.Utils
{
    public static class LocalizationManager
    {
        public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.RU;

        private static readonly Dictionary<string, Dictionary<AppLanguage, string>> Strings = new()
        {
            { "Title", new() { { AppLanguage.RU, "RunCrypt - Современное шифрование" }, { AppLanguage.EN, "RunCrypt - Modern Encryption" } } },
            { "File", new() { { AppLanguage.RU, "Файл:" }, { AppLanguage.EN, "File:" } } },
            { "Browse", new() { { AppLanguage.RU, "Обзор..." }, { AppLanguage.EN, "Browse..." } } },
            { "Algorithm", new() { { AppLanguage.RU, "Алгоритм:" }, { AppLanguage.EN, "Algorithm:" } } },
            { "Password", new() { { AppLanguage.RU, "Пароль:" }, { AppLanguage.EN, "Password:" } } },
            { "Encrypt", new() { { AppLanguage.RU, "Зашифровать" }, { AppLanguage.EN, "Encrypt" } } },
            { "Decrypt", new() { { AppLanguage.RU, "Расшифровать" }, { AppLanguage.EN, "Decrypt" } } },
            { "SecureDelete", new() { { AppLanguage.RU, "Безопасно удалить оригинал (Шреддинг)" }, { AppLanguage.EN, "Securely delete original (Shredding)" } } },
            { "DragDrop", new() { { AppLanguage.RU, "Перетащите файл сюда" }, { AppLanguage.EN, "Drag and drop file here" } } }
        };

        public static string GetString(string key) => Strings.ContainsKey(key) ? Strings[key][CurrentLanguage] : key;
    }
}