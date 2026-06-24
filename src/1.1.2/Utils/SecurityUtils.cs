using System;
using System.IO;
using System.Linq;

namespace RunCrypt.Utils
{
    public static class SecurityUtils
    {
        public static int GetPasswordStrengthScore(string password)
        {
            if (string.IsNullOrEmpty(password)) return -1;
            
            int score = 0;
            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;
            if (password.Any(char.IsUpper)) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

            return score;
        }

        public static void SecureDelete(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                byte[] dummyBuffer = new byte[4096];
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    long length = fs.Length;
                    for (long i = 0; i < length; i += dummyBuffer.Length)
                    {
                        int toWrite = (int)Math.Min(dummyBuffer.Length, length - i);
                        fs.Write(dummyBuffer, 0, toWrite);
                    }
                    fs.Flush();
                }
                File.Delete(filePath);
            }
            catch { /* Игнорируем ошибки доступа */ }
        }
    }
}