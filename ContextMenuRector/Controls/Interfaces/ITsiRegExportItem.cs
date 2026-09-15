using ContextMenuRector.BluePointLilac.Methods;
using ContextMenuRector.Methods;
using System;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuRector.Controls.Interfaces
{
    interface ITsiRegExportItem
    {
        string Text { get; set; }
        string RegPath { get; }
        ContextMenuStrip ContextMenuStrip { get; set; }
        RegExportMenuItem TsiRegExport { get; set; }
    }

    sealed class RegExportMenuItem : ToolStripMenuItem
    {
        public RegExportMenuItem(ITsiRegExportItem item) : base(AppString.Menu.ExportRegistry)
        {
            item.ContextMenuStrip.Opening += (sender, e) =>
            {
                using(var key = RegistryEx.GetRegistryKey(item.RegPath))
                    this.Visible = key != null;
            };
            this.Click += (sender, e) =>
            {
                using(SaveFileDialog dlg = new SaveFileDialog())
                {
                    string date = DateTime.Today.ToString("yyyy-MM-dd");
                    string time = DateTime.Now.ToString("HH.mm.ss");
                    string filePath = $@"{AppConfig.BackupDir}\{date}\{item.Text} - {time}.reg";
                    string dirPath = Path.GetDirectoryName(filePath);
                    string fileName = Path.GetFileName(filePath);
                    Directory.CreateDirectory(dirPath);
                    dlg.FileName = fileName;
                    dlg.InitialDirectory = dirPath;
                    dlg.Filter = $"{AppString.Dialog.RegistryFile}|*.reg";
                    if(dlg.ShowDialog() == DialogResult.OK)
                    {
                        if(!ExternalProgram.ExportRegistry(item.RegPath, dlg.FileName))
                        {
                            AppMessageBox.Show(AppString.Message.RegEditExportFailed,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    if(Directory.GetFileSystemEntries(dirPath).Length == 0)
                    {
                        Directory.Delete(dirPath);
                    }
                }
            };
        }
    }
}