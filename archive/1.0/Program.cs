using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

namespace SeedEncryptor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private TextBox txtFilePath = null!;
        private TextBox txtKey = null!;
        private ComboBox cmbEncryptType = null!;
        private ListBox lstLog = null!;
        private Button btnBrowse = null!;
        private Button btnEncrypt = null!;
        private Button btnDecrypt = null!;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Seed Crypto Engine Pro";
            this.Size = new System.Drawing.Size(680, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblFile = new Label { Text = "Путь к файлу:", Location = new System.Drawing.Point(15, 20), Size = new System.Drawing.Size(100, 20) };
            txtFilePath = new TextBox { Location = new System.Drawing.Point(120, 17), Size = new System.Drawing.Size(410, 23) };
            btnBrowse = new Button { Text = "Обзор...", Location = new System.Drawing.Point(540, 16), Size = new System.Drawing.Size(100, 25) };
            btnBrowse.Click += BtnBrowse_Click;

            Label lblType = new Label { Text = "Алгоритм:", Location = new System.Drawing.Point(15, 60), Size = new System.Drawing.Size(100, 20) };
            cmbEncryptType = new ComboBox { Location = new System.Drawing.Point(120, 57), Size = new System.Drawing.Size(410, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            
            cmbEncryptType.Items.AddRange(new string[] { 
                "[Без ключа] Base64 Контейнеризация", 
                "[Без ключа] Инверсия байтового потока (NOT-Cipher)", 
                "[Без ключа] Статический Матричный XOR", 
                "[С ключом] AES-256 (Высокая стойкость / PBKDF2)", 
                "[С ключом] Triple DES (Классическое блочное шифрование)",
                "[С ключом] Динамический кастомный XOR-Каскад",
                "[С ключом] Комбинированный AES + Кастомный Сдвиг"
            });
            cmbEncryptType.SelectedIndex = 3; 
            cmbEncryptType.SelectedIndexChanged += CmbEncryptType_SelectedIndexChanged;

            Label lblKey = new Label { Text = "Ключ / Пароль:", Location = new System.Drawing.Point(15, 100), Size = new System.Drawing.Size(100, 20) };
            txtKey = new TextBox { Location = new System.Drawing.Point(120, 97), Size = new System.Drawing.Size(410, 23), PasswordChar = '*' };

            btnEncrypt = new Button { Text = "Зашифровать в .seed", Location = new System.Drawing.Point(15, 140), Size = new System.Drawing.Size(300, 35) };
            btnEncrypt.Click += BtnEncrypt_Click;

            btnDecrypt = new Button { Text = "Расшифровать из .seed", Location = new System.Drawing.Point(340, 140), Size = new System.Drawing.Size(300, 35) };
            btnDecrypt.Click += BtnDecrypt_Click;

            lstLog = new ListBox { Location = new System.Drawing.Point(15, 190), Size = new System.Drawing.Size(625, 230) };

            this.Controls.AddRange(new Control[] { lblFile, txtFilePath, btnBrowse, lblType, cmbEncryptType, lblKey, txtKey, btnEncrypt, btnDecrypt, lstLog });
            
            EvaluateKeyFieldState();
        }

        private void Log(string message)
        {
            lstLog.Items.Add($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
            lstLog.SelectedIndex = lstLog.Items.Count - 1;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Все файлы (*.*)|*.*|Файлы сидов (*.seed)|*.seed";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
            }
        }

        private void CmbEncryptType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            EvaluateKeyFieldState();
        }

        private void EvaluateKeyFieldState()
        {
            // Индексы 0, 1, 2 не требуют ключа
            bool requiresKey = cmbEncryptType.SelectedIndex >= 3;
            txtKey.Enabled = requiresKey;
            if (!requiresKey)
            {
                txtKey.Text = string.Empty;
            }
        }

        private void BtnEncrypt_Click(object? sender, EventArgs e)
        {
            string inputPath = txtFilePath.Text;
            if (!File.Exists(inputPath))
            {
                MessageBox.Show("Исходный файл не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int algorithmIndex = cmbEncryptType.SelectedIndex;
            string key = txtKey.Text;

            if (algorithmIndex >= 3 && string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Для выбранного алгоритма необходимо указать ключ шифрования!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lstLog.Items.Clear();
            Log($"Старт шифрования: {Path.GetFileName(inputPath)}");
            
            string outputPath = Path.ChangeExtension(inputPath, ".seed");

            try
            {
                byte[] originalData = File.ReadAllBytes(inputPath);
                byte[] encryptedData = null!;
                byte[] salt = new byte[16];
                
                if (algorithmIndex >= 3)
                {
                    RandomNumberGenerator.Fill(salt);
                }

                Log("Запущено предварительное тестирование структуры данных...");
                
                encryptedData = ExecuteEncryption(originalData, algorithmIndex, key, salt);
                
                byte[] verifyData = ExecuteDecryption(encryptedData, algorithmIndex, key, salt);
                
                if (!CompareByteArray(originalData, verifyData))
                {
                    Log("Критический сбой теста: Дешифрованный тестовый блок не совпадает с оригиналом!");
                    MessageBox.Show("Внутренний тест целостности провален. Операция отменена.", "Сбой", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                Log("Тест точности пройден. Выполняется финализация контейнера .seed...");

                using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(Encoding.UTF8.GetBytes("SDCR")); 
                    writer.Write((byte)algorithmIndex);          
                    writer.Write(Path.GetExtension(inputPath));  
                    writer.Write(salt);                          
                    writer.Write(encryptedData.Length);          
                    writer.Write(encryptedData);                 
                }

                Log($"Файл зашифрован и сохранен в: {Path.GetFileName(outputPath)}");
                MessageBox.Show("Шифрование выполнено успешно. Данные верифицированы.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"Ошибка шифрования: {ex.Message}");
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDecrypt_Click(object? sender, EventArgs e)
        {
            string inputPath = txtFilePath.Text;
            if (!File.Exists(inputPath))
            {
                MessageBox.Show("Файл .seed не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lstLog.Items.Clear();
            Log($"Чтение контейнера: {Path.GetFileName(inputPath)}");

            try
            {
                using FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
                using BinaryReader reader = new BinaryReader(fs);

                string magic = Encoding.UTF8.GetString(reader.ReadBytes(4));
                if (magic != "SDCR")
                {
                    Log("Ошибка: Файл не является валидным контейнером Seed Crypto Engine.");
                    MessageBox.Show("Неверный формат или заголовок файла!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int algorithmIndex = reader.ReadByte();
                string ext = reader.ReadString();
                byte[] salt = reader.ReadBytes(16);
                int dataLength = reader.ReadInt32();
                byte[] encryptedData = reader.ReadBytes(dataLength);

                Log($"Обнаружен алгоритм: {cmbEncryptType.Items[algorithmIndex]}");
                
                string key = txtKey.Text;
                if (algorithmIndex >= 3 && string.IsNullOrEmpty(key))
                {
                    Log("Ошибка: Для расшифровки данного файла требуется ввести ключ в поле ввода.");
                    MessageBox.Show("Введите ключ шифрования!", "Требуется пароль", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Log("Проведение дешифрации байт-потока...");
                byte[] decryptedData = ExecuteDecryption(encryptedData, algorithmIndex, key, salt);

                string targetPath = Path.ChangeExtension(inputPath, "_decrypted" + ext);
                File.WriteAllBytes(targetPath, decryptedData);

                Log($"Структура оригинального файла воссоздана: {Path.GetFileName(targetPath)}");
                MessageBox.Show("Файл успешно дешифрован и восстановлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"Ошибка дешифрации: {ex.Message}. Возможно, указан неверный ключ.");
                MessageBox.Show("Не удалось расшифровать файл. Проверьте правильность ключа.", "Ошибка крипто-движка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private byte[] ExecuteEncryption(byte[] data, int algorithm, string key, byte[] salt)
        {
            return algorithm switch
            {
                0 => Encoding.UTF8.GetBytes(Convert.ToBase64String(data)),
                1 => InvertBytes(data),
                2 => StaticXorMatrix(data),
                3 => ProcessAes256(data, key, salt, true),
                4 => ProcessTripleDes(data, key, salt, true),
                5 => CustomXorCascade(data, key, true),
                6 => CustomXorCascade(ProcessAes256(data, key, salt, true), key, true),
                _ => throw new InvalidOperationException("Неизвестный тип алгоритма")
            };
        }

        private byte[] ExecuteDecryption(byte[] data, int algorithm, string key, byte[] salt)
        {
            return algorithm switch
            {
                0 => Convert.FromBase64String(Encoding.UTF8.GetString(data)),
                1 => InvertBytes(data), // Инверсия симметрична
                2 => StaticXorMatrix(data), // XOR симметричен
                3 => ProcessAes256(data, key, salt, false),
                4 => ProcessTripleDes(data, key, salt, false),
                5 => CustomXorCascade(data, key, false),
                6 => ProcessAes256(CustomXorCascade(data, key, false), key, salt, false),
                _ => throw new InvalidOperationException("Неизвестный тип алгоритма")
            };
        }


        private byte[] InvertBytes(byte[] data)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(~data[i]);
            }
            return result;
        }

        private byte[] StaticXorMatrix(byte[] data)
        {
            byte[] mask = new byte[] { 0xA5, 0x5A, 0xF0, 0x0F, 0xCC, 0x33, 0xAA, 0x55 };
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ mask[i % mask.Length]);
            }
            return result;
        }


        private byte[] ProcessAes256(byte[] data, string key, byte[] salt, bool encrypt)
        {
            using Aes aes = Aes.Create();
            aes.KeySize = 256;

            using var deriveBytes = new Rfc2898DeriveBytes(key, salt, 50000, HashAlgorithmName.SHA256);
            aes.Key = deriveBytes.GetBytes(32);
            aes.IV = deriveBytes.GetBytes(16);

            using MemoryStream ms = new MemoryStream();
            using (ICryptoTransform transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor())
            using (CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }
            return ms.ToArray();
        }

        private byte[] ProcessTripleDes(byte[] data, string key, byte[] salt, bool encrypt)
        {
            using TripleDES tdes = TripleDES.Create();
            using var deriveBytes = new Rfc2898DeriveBytes(key, salt, 20000, HashAlgorithmName.SHA256);
            tdes.Key = deriveBytes.GetBytes(24);
            tdes.IV = deriveBytes.GetBytes(8);

            using MemoryStream ms = new MemoryStream();
            using (ICryptoTransform transform = encrypt ? tdes.CreateEncryptor() : tdes.CreateDecryptor())
            using (CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }
            return ms.ToArray();
        }

        private byte[] CustomXorCascade(byte[] data, string key, bool encrypt)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] result = new byte[data.Length];
            
            byte[] keyHash = SHA256.HashData(keyBytes);

            for (int i = 0; i < data.Length; i++)
            {
                byte keyByte = keyBytes[i % keyBytes.Length];
                byte hashModifier = keyHash[i % keyHash.Length];
                
                if (encrypt)
                {
                    byte stepped = (byte)(data[i] + hashModifier);
                    result[i] = (byte)(stepped ^ keyByte);
                }
                else
                {
                    byte unXor = (byte)(data[i] ^ keyByte);
                    result[i] = (byte)(unXor - hashModifier);
                }
            }
            return result;
        }

        private bool CompareByteArray(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}