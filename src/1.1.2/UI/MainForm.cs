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
            UpdatePasswordStrengthDisplay();
        }

        private void InitializeComponent()
        {
            // Увеличили форму для придания «премиального» и просторного вида
            this.Size = new Size(800, 580);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Поддержка Drag & Drop
            this.AllowDrop = true;
            this.DragEnter += (s, e) => { if (e.Data!.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            this.DragDrop += (s, e) => {
                string[] files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0) txtFilePath.Text = files[0];
            };

            Font mainFont = new Font("Segoe UI", 10F, FontStyle.Regular);
            Font labelFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            // Строка файла
            lblFile = new Label { Location = new Point(20, 25), Size = new Size(130, 25), TextAlign = ContentAlignment.MiddleRight, Font = labelFont };
            txtFilePath = new TextBox { Location = new Point(165, 24), Size = new Size(470, 27), Font = mainFont };
            btnBrowse = new Button { Location = new Point(650, 23), Size = new Size(110, 30), Font = mainFont };
            btnBrowse.Click += (s, e) => {
                using var ofd = new OpenFileDialog { Filter = "All files (*.*)|*.*|RunCrypt Archives (*.rcrp)|*.rcrp" };
                if (ofd.ShowDialog() == DialogResult.OK) txtFilePath.Text = ofd.FileName;
            };

            // Строка алгоритма
            lblAlgorithm = new Label { Location = new Point(20, 75), Size = new Size(130, 25), TextAlign = ContentAlignment.MiddleRight, Font = labelFont };
            cmbAlgorithm = new ComboBox { Location = new Point(165, 74), Size = new Size(595, 27), DropDownStyle = ComboBoxStyle.DropDownList, Font = mainFont };
            cmbAlgorithm.Items.AddRange(new string[] { 
                "AES-256-GCM (Modern Secure Standard / AEAD)", 
                "ChaCha20-Poly1305 (Ultra Fast Standard / AEAD)",
                "[Experimental] Base64 Containerization", 
                "[Experimental] Byte Inversion (NOT)", 
                "[Experimental] Dynamic Custom XOR Cascade"
            });
            cmbAlgorithm.SelectedIndex = 0;
            cmbAlgorithm.SelectedIndexChanged += (s, e) => EvaluateKeyField();

            // Строка пароля
            lblPassword = new Label { Location = new Point(20, 125), Size = new Size(130, 25), TextAlign = ContentAlignment.MiddleRight, Font = labelFont };
            txtKey = new TextBox { Location = new Point(165, 124), Size = new Size(595, 27), PasswordChar = '•', Font = mainFont };
            txtKey.TextChanged += (s, e) => UpdatePasswordStrengthDisplay();

            // Индикатор силы пароля теперь НАХОДИТСЯ СНИЗУ поля и имеет запас на всю ширину
            lblPasswordStrength = new Label { Location = new Point(165, 156), Size = new Size(595, 20), Font = new Font("Segoe UI", 9F, FontStyle.Italic) };

            // Шреддинг чекбокс
            chkSecureDelete = new CheckBox { Location = new Point(165, 185), Size = new Size(595, 25), Font = mainFont, FlatStyle = FlatStyle.Flat };

            // Кнопки управления (Большие, современные)
            Font buttonFont = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnEncrypt = new Button { Location = new Point(165, 225), Size = new Size(290, 45), Font = buttonFont, Cursor = Cursors.Hand };
            btnEncrypt.Click += (s, e) => ProcessAction(true);

            btnDecrypt = new Button { Location = new Point(470, 225), Size = new Size(290, 45), Font = buttonFont, Cursor = Cursors.Hand };
            btnDecrypt.Click += (s, e) => ProcessAction(false);

            // Окно логов с красивым моноширинным шрифтом
            lstLog = new ListBox { Location = new Point(25, 290), Size = new Size(735, 180), Font = new Font("Consolas", 9.5F, FontStyle.Regular) };

            // Кнопки смены тем/языков (внизу слева)
            btnToggleTheme = new Button { Text = "🌙 / ☀️", Location = new Point(25, 490), Size = new Size(70, 30), Font = mainFont };
            btnToggleTheme.Click += (s, e) => {
                ThemeManager.ApplyTheme(this, ThemeManager.CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
                UpdatePasswordStrengthDisplay(); // Обновляем цвета шрифта под новую тему
            };

            btnToggleLang = new Button { Text = "🌐 EN/RU", Location = new Point(105, 490), Size = new Size(90, 30), Font = mainFont };
            btnToggleLang.Click += (s, e) => {
                LocalizationManager.CurrentLanguage = LocalizationManager.CurrentLanguage == AppLanguage.RU ? AppLanguage.EN : AppLanguage.RU;
                ApplyLocalization();
                UpdatePasswordStrengthDisplay(); // Исправлен баг: теперь мгновенно переводит текст силы пароля
            };

            this.Controls.AddRange(new Control[] { 
                lblFile, txtFilePath, btnBrowse, lblAlgorithm, cmbAlgorithm, 
                lblPassword, txtKey, lblPasswordStrength, chkSecureDelete, 
                btnEncrypt, btnDecrypt, lstLog, btnToggleTheme, btnToggleLang 
            });
            
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

        private void UpdatePasswordStrengthDisplay()
        {
            int score = SecurityUtils.GetPasswordStrengthScore(txtKey.Text);
            lblPasswordStrength.Text = LocalizationManager.GetString($"Str_{score}");

            // Динамическое окрашивание текста в зависимости от надежности и темы
            bool isDark = ThemeManager.CurrentTheme == AppTheme.Dark;
            lblPasswordStrength.ForeColor = score switch
            {
                <= 2 => isDark ? Color.FromArgb(255, 100, 100) : Color.Red,       // Слабый
                3 => isDark ? Color.FromArgb(255, 180, 50) : Color.DarkOrange,    // Средний
                _ => isDark ? Color.FromArgb(100, 255, 100) : Color.DarkGreen     // Надежный
            };
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

        private void ProcessAction(bool isEncrypt)
        {
            string inputPath = txtFilePath.Text;
            if (!File.Exists(inputPath))
            {
                MessageBox.Show(LocalizationManager.GetString("Text_FileNotExist"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CryptoAlgorithm algo = (CryptoAlgorithm)cmbAlgorithm.SelectedIndex;
            string key = txtKey.Text;

            if (txtKey.Enabled && string.IsNullOrEmpty(key))
            {
                MessageBox.Show(LocalizationManager.GetString("Text_PasswordRequired"), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lstLog.Items.Clear();
            string actionStr = isEncrypt ? "Encryption" : "Decryption";
            Log($"Starting {actionStr} for file: {Path.GetFileName(inputPath)}");

            string outputPath = isEncrypt ? Path.ChangeExtension(inputPath, ".rcrp") : Path.ChangeExtension(inputPath, "_decrypted.tmp");

            try
            {
                Cursor = Cursors.WaitCursor;
                CryptoEngine.ProcessFile(inputPath, outputPath, key, algo, isEncrypt);
                
                if (chkSecureDelete.Checked)
                {
                    Log("Executing secure file shredding...");
                    SecurityUtils.SecureDelete(inputPath);
                }

                Log("Operation completed successfully!");
                MessageBox.Show(LocalizationManager.GetString("Text_Success"), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (CryptographicException)
            {
                Log("Crypto Error: Invalid password or corrupted container (AEAD Integrity Tag Mismatch)!");
                MessageBox.Show("Invalid password or modified file data!", "Security Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Log($"Critical Error: {ex.Message}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}