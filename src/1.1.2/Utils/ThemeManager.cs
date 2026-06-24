using System.Drawing;
using System.Windows.Forms;
using RunCrypt.Models;

namespace RunCrypt.Utils
{
    public static class ThemeManager
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

        public static void ApplyTheme(Form form, AppTheme theme)
        {
            CurrentTheme = theme;
            bool isDark = theme == AppTheme.Dark;

            Color bg = isDark ? Color.FromArgb(30, 30, 30) : Color.WhiteSmoke;
            Color fg = isDark ? Color.White : Color.Black;
            Color controlBg = isDark ? Color.FromArgb(45, 45, 48) : Color.White;

            form.BackColor = bg;
            form.ForeColor = fg;

            foreach (Control ctrl in form.Controls)
            {
                UpdateControlTheme(ctrl, bg, fg, controlBg, isDark);
            }
        }

        private static void UpdateControlTheme(Control ctrl, Color bg, Color fg, Color controlBg, bool isDark)
        {
            if (ctrl is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = isDark ? Color.FromArgb(0, 122, 204) : Color.FromArgb(0, 120, 215);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 0;
            }
            else if (ctrl is TextBox || ctrl is ComboBox || ctrl is ListBox)
            {
                ctrl.BackColor = controlBg;
                ctrl.ForeColor = fg;
                if (ctrl is ComboBox cb) cb.FlatStyle = FlatStyle.Flat;
            }
            else if (ctrl is Label || ctrl is CheckBox)
            {
                ctrl.ForeColor = fg;
            }

            foreach (Control child in ctrl.Controls) UpdateControlTheme(child, bg, fg, controlBg, isDark);
        }
    }
}