using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using RunCrypt.Crypto;
using RunCrypt.Models;
using RunCrypt.Utils;

namespace RunCrypt.UI
{
    public class MainForm : Form
    {
        private TextBox txtFilePath = null!;
        private TextBox txtKey = null!;
        private ComboBox cmbAlgorithm = null!;
        private ListBox lstLog = null!;
        private Button btnBrowse = null!;
        private Button btnEncrypt = null!;
        private Button btnDecrypt = null!;
        private CheckBox chkSecureDelete = null!;
        private Label lblPasswordStrength = null!;
        private Button btnToggleTheme = null!;
        private Button btnToggleLang = null!;
        private Label lblFile = null!, lblAlgorithm = null!, lblPassword = null!;

        public MainForm()
        {
            InitializeComponent();
            ApplyLocalization();
            ThemeManager.ApplyTheme(this, AppTheme.Dark);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(720, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            lblFile = new Label { Location = new Point(15, 20), Size = new Size(100, 20) };
            txtFilePath = new TextBox { Location = new Point(120, 17), Size = new Size(450, 23) };
            btnBrowse = new Button { Location = new Point(580, 16), Size = new Size(100, 25) };
            btnBrowse.Click += (s, e) => {
                using var ofd = new OpenFileDialog { Filter = "All files (*.*)|*.*|RunCrypt Archives (*.rcrp)|*.rcrp" };
                if (ofd.ShowDialog() == DialogResult.OK) txtFilePath.Text = ofd.FileName;
            };

            lblAlgorithm = new Label { Location = new Point(15, 60), Size = new Size(100, 20) };
            cmbAlgorithm = new ComboBox { Location = new Point(120, 57), Size = new Size(450, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAlgorithm.Items.AddRange(new string[] { 
                "AES-256-GCM (Современный стандарт / AEAD)", 
                "ChaCha20-Poly1305 (Высокая скорость / AEAD)",
                "[Exp] Base64 Контейнеризация", 
                "[Exp] Инверсия байтов (NOT)", 
                "[Exp] Кастомный XOR"
            });
            cmbAlgorithm.SelectedIndex = 0;
            cmbAlgorithm.SelectedIndexChanged += (s, e) => EvaluateKeyField();

            lblPassword = new Label { Location = new Point(15, 100), Size = new Size(100, 20) };
            txtKey = new TextBox { Location = new Point(120, 97), Size = new Size(450, 23), PasswordChar = '•' };
            txtKey.TextChanged += (s, e) => lblPasswordStrength.Text = SecurityUtils.CheckPasswordStrength(txtKey.Text);

            lblPasswordStrength = new Label { Location = new Point(580, 100), Size = new Size(100, 20), ForeColor = Color.Gray };

            chkSecureDelete = new CheckBox { Location = new Point(120, 130), Size = new Size(400, 20) };

            btnEncrypt = new Button { Location = new Point(15, 170), Size = new Size(330, 40) };
            btnEncrypt.Click += (s, e) => ProcessAction(true);

            btnDecrypt = new Button { Location = new Point(355, 170), Size = new Size(330, 40) };
            btnDecrypt.Click += (s, e) => ProcessAction(false);

            lstLog = new ListBox { Location = new Point(15, 225), Size = new Size(670, 180) };

            btnToggleTheme = new Button { Text = "🌙/☀️", Location = new Point(15, 420), Size = new Size(50, 25) };
            btnToggleTheme.Click += (s, e) => ThemeManager.ApplyTheme(this, ThemeManager.CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);

            btnToggleLang = new Button { Text = "RU/EN", Location = new Point(75, 420), Size = new Size(50, 25) };
            btnToggleLang.Click += (s, e) => {
                LocalizationManager.CurrentLanguage = LocalizationManager.CurrentLanguage == AppLanguage.RU ? AppLanguage.EN : AppLanguage.RU;
                ApplyLocalization();
            };

            this.Controls.AddRange(new Control[] { lblFile, txtFilePath, btnBrowse, lblAlgorithm, cmbAlgorithm, lblPassword, txtKey, lblPasswordStrength, chkSecureDelete, btnEncrypt, btnDecrypt, lstLog, btnToggleTheme, btnToggleLang });
            EvaluateKeyField();
        }

        private void ApplyLocalization()
        {
            this.Text = LocalizationManager.GetString("Title");
            lblFile.Text = LocalizationManager.GetString("File");
            btnBrowse.Text = LocalizationManager.GetString("Browse");
            lblAlgorithm.Text = LocalizationManager.GetString("Algorithm");
            lblPassword.Text = LocalizationManager.GetString("Password");
            btnEncrypt.Text = LocalizationManager.GetString("Encrypt");
            btnDecrypt.Text = LocalizationManager.GetString("Decrypt");
            chkSecureDelete.Text = LocalizationManager.GetString("SecureDelete");
        }

        private void EvaluateKeyField()
        {
            bool requiresKey = cmbAlgorithm.SelectedIndex <= 1 || cmbAlgorithm.SelectedIndex == 4;
            txtKey.Enabled = requiresKey;
            if (!requiresKey) txtKey.Text = string.Empty;
        }

        private void Log(string msg)
        {
            lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        private void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data!.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
            if (files.Length > 0) txtFilePath.Text = files[0];
        }

        private void ProcessAction(bool isEncrypt)
        {
            string inputPath = txtFilePath.Text;
            if (!File.Exists(inputPath))
            {
                MessageBox.Show("Файл не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CryptoAlgorithm algo = (CryptoAlgorithm)cmbAlgorithm.SelectedIndex;
            string key = txtKey.Text;

            if (txtKey.Enabled && string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Введите пароль!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lstLog.Items.Clear();
            string actionStr = isEncrypt ? "Шифрование" : "Дешифрация";
            Log($"Старт: {actionStr} файла {Path.GetFileName(inputPath)}");

            string outputPath = isEncrypt ? Path.ChangeExtension(inputPath, ".rcrp") : Path.ChangeExtension(inputPath, "_decrypted.tmp");

            try
            {
                Cursor = Cursors.WaitCursor;
                CryptoEngine.ProcessFile(inputPath, outputPath, key, algo, isEncrypt);
                
                if (chkSecureDelete.Checked)
                {
                    Log("Выполняется безопасное уничтожение оригинала...");
                    SecurityUtils.SecureDelete(inputPath);
                }

                Log("Операция успешно завершена!");
                MessageBox.Show($"{actionStr} выполнено успешно.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (CryptographicException)
            {
                Log("Ошибка: Неверный пароль или поврежденные данные (AEAD Tag Mismatch)!");
                MessageBox.Show("Неверный пароль или файл был изменен!", "Ошибка безопасности", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Log($"Критическая ошибка: {ex.Message}");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}