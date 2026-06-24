using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using RunCrypt.Models;

namespace RunCrypt.Crypto
{
    public static class CryptoEngine
    {
        private const int SaltSize = 16;
        private const int NonceSize = 12; // Для AES-GCM и ChaCha20
        private const int TagSize = 16;   // Тег аутентификации

        public static void ProcessFile(string inputPath, string outputPath, string password, CryptoAlgorithm algorithm, bool isEncrypt)
        {
            byte[] fileData = File.ReadAllBytes(inputPath);
            byte[] resultData;

            if (isEncrypt)
            {
                resultData = Encrypt(fileData, password, algorithm);
                
                using FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                using BinaryWriter writer = new BinaryWriter(fs);
                writer.Write(Encoding.UTF8.GetBytes("RCRP")); // RunCrypt Magic Header
                writer.Write((byte)algorithm);
                writer.Write(Path.GetExtension(inputPath));
                writer.Write(resultData.Length);
                writer.Write(resultData);
            }
            else
            {
                using FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
                using BinaryReader reader = new BinaryReader(fs);

                string magic = Encoding.UTF8.GetString(reader.ReadBytes(4));
                if (magic != "RCRP") throw new Exception("Неверный формат файла контейнера!");

                CryptoAlgorithm algo = (CryptoAlgorithm)reader.ReadByte();
                string ext = reader.ReadString();
                int dataLength = reader.ReadInt32();
                byte[] encryptedData = reader.ReadBytes(dataLength);

                resultData = Decrypt(encryptedData, password, algo);
                
                // Перезаписываем outputPath с правильным расширением
                outputPath = Path.ChangeExtension(outputPath, ext);
                File.WriteAllBytes(outputPath, resultData);
            }
        }

        private static byte[] Encrypt(byte[] data, string password, CryptoAlgorithm algo)
        {
            if (algo == CryptoAlgorithm.Experimental_Base64) return Encoding.UTF8.GetBytes(Convert.ToBase64String(data));
            if (algo == CryptoAlgorithm.Experimental_NotCipher) return InvertBytes(data);
            if (algo == CryptoAlgorithm.Experimental_XorCascade) return CustomXorCascade(data, password);

            byte[] salt = new byte[SaltSize];
            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(salt);
            RandomNumberGenerator.Fill(nonce);

            byte[] key = DeriveKey(password, salt);
            byte[] tag = new byte[TagSize];
            byte[] ciphertext = new byte[data.Length];

            if (algo == CryptoAlgorithm.AesGcm256)
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Encrypt(nonce, data, ciphertext, tag);
            }
            else if (algo == CryptoAlgorithm.ChaCha20Poly1305)
            {
                using var chacha = new ChaCha20Poly1305(key);
                chacha.Encrypt(nonce, data, ciphertext, tag);
            }

            // Упаковываем: Salt + Nonce + Tag + Ciphertext
            byte[] result = new byte[SaltSize + NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(nonce, 0, result, SaltSize, NonceSize);
            Buffer.BlockCopy(tag, 0, result, SaltSize + NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, SaltSize + NonceSize + TagSize, ciphertext.Length);

            return result;
        }

        private static byte[] Decrypt(byte[] data, string password, CryptoAlgorithm algo)
        {
            if (algo == CryptoAlgorithm.Experimental_Base64) return Convert.FromBase64String(Encoding.UTF8.GetString(data));
            if (algo == CryptoAlgorithm.Experimental_NotCipher) return InvertBytes(data);
            if (algo == CryptoAlgorithm.Experimental_XorCascade) return CustomXorCascade(data, password);

            byte[] salt = new byte[SaltSize];
            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            byte[] ciphertext = new byte[data.Length - SaltSize - NonceSize - TagSize];

            Buffer.BlockCopy(data, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(data, SaltSize, nonce, 0, NonceSize);
            Buffer.BlockCopy(data, SaltSize + NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(data, SaltSize + NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

            byte[] key = DeriveKey(password, salt);
            byte[] plaintext = new byte[ciphertext.Length];

            if (algo == CryptoAlgorithm.AesGcm256)
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }
            else if (algo == CryptoAlgorithm.ChaCha20Poly1305)
            {
                using var chacha = new ChaCha20Poly1305(key);
                chacha.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return plaintext;
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password ?? "", salt, 200000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256 bit key
        }

        private static byte[] InvertBytes(byte[] data)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++) result[i] = (byte)~data[i];
            return result;
        }

        private static byte[] CustomXorCascade(byte[] data, string key)
        {
            if (string.IsNullOrEmpty(key)) return data;
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++) result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            return result;
        }
    }
}