using ContextMenuRector.BluePointLilac.Methods;
using ContextMenuRector.Methods;
using System;
using System.Windows.Forms;

namespace ContextMenuRector
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware); // Equivalent to <dpiAware>true</dpiAware> in App.manifest
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (SingleInstance.IsRunning()) return;
            AppString.LoadStrings();
            Updater.PeriodicUpdate();
            XmlDicHelper.ReloadDics();
            Application.Run(new MainForm());
        }
    }
}
