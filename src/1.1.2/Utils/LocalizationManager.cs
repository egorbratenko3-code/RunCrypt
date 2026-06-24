using System.Collections.Generic;
using RunCrypt.Models;

namespace RunCrypt.Utils
{
    public static class LocalizationManager
    {
        // Устанавливаем изначальный язык — Английский
        public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.EN;

        private static readonly Dictionary<string, Dictionary<AppLanguage, string>> Strings = new()
        {
            { "Title", new() { { AppLanguage.RU, "RunCrypt - Современное шифрование" }, { AppLanguage.EN, "RunCrypt - Modern Encryption" } } },
            { "File", new() { { AppLanguage.RU, "Путь к файлу:" }, { AppLanguage.EN, "File Path:" } } },
            { "Browse", new() { { AppLanguage.RU, "Обзор..." }, { AppLanguage.EN, "Browse..." } } },
            { "Algorithm", new() { { AppLanguage.RU, "Алгоритм:" }, { AppLanguage.EN, "Algorithm:" } } },
            { "Password", new() { { AppLanguage.RU, "Пароль / Ключ:" }, { AppLanguage.EN, "Password / Key:" } } },
            { "Encrypt", new() { { AppLanguage.RU, "Зашифровать в .rcrp" }, { AppLanguage.EN, "Encrypt to .rcrp" } } },
            { "Decrypt", new() { { AppLanguage.RU, "Расшифровать из .rcrp" }, { AppLanguage.EN, "Decrypt from .rcrp" } } },
            { "SecureDelete", new() { { AppLanguage.RU, "Безопасное удаление оригинала (Шреддинг)" }, { AppLanguage.EN, "Securely delete original file (Shredding)" } } },
            { "Text_FileNotExist", new() { { AppLanguage.RU, "Указанный файл не найден!" }, { AppLanguage.EN, "Selected file does not exist!" } } },
            { "Text_PasswordRequired", new() { { AppLanguage.RU, "Для этого алгоритма необходим пароль!" }, { AppLanguage.EN, "Password is required for this algorithm!" } } },
            
            // Исправленная строка: убрано третье слово
            { "Text_Success", new() { { AppLanguage.RU, "Операция успешно завершена." }, { AppLanguage.EN, "Operation completed successfully." } } },
            
            // Локализация силы пароля
            { "Str_-1", new() { { AppLanguage.RU, "" }, { AppLanguage.EN, "" } } },
            { "Str_0", new() { { AppLanguage.RU, "Показатель: Слишком короткий / небезопасный" }, { AppLanguage.EN, "Strength: Too short / unsafe" } } },
            { "Str_1", new() { { AppLanguage.RU, "Показатель: Очень слабый пароль" }, { AppLanguage.EN, "Strength: Very Weak" } } },
            { "Str_2", new() { { AppLanguage.RU, "Показатель: Слабый пароль" }, { AppLanguage.EN, "Strength: Weak" } } },
            { "Str_3", new() { { AppLanguage.RU, "Показатель: Средний уровень" }, { AppLanguage.EN, "Strength: Medium" } } },
            { "Str_4", new() { { AppLanguage.RU, "Показатель: Надежный пароль" }, { AppLanguage.EN, "Strength: Strong" } } },
            { "Str_5", new() { { AppLanguage.RU, "Показатель: Идеальный пароль!" }, { AppLanguage.EN, "Strength: Excellent!" } } }
        };

        public static string GetString(string key) => Strings.ContainsKey(key) ? Strings[key][CurrentLanguage] : key;
    }
}