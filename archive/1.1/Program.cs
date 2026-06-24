using System;
using System.Windows.Forms;
using RunCrypt.UI;

namespace RunCrypt
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
}