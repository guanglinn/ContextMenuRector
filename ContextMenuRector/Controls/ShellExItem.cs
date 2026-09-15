using ContextMenuRector.BluePointLilac.Methods;
using ContextMenuRector.Controls.Interfaces;
using ContextMenuRector.Methods;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace ContextMenuRector.Controls
{
    sealed class ShellExItem : FoldSubItem, IChkVisibleItem, IBtnShowMenuItem, ITsiGuidItem,
        ITsiWebSearchItem, ITsiFilePathItem, ITsiRegPathItem, ITsiRegDeleteItem, ITsiRegExportItem, IProtectOpenItem
    {
        public static Dictionary<string, Guid> GetPathAndGuids(string shellExPath, bool isDragDrop = false)
        {
            Dictionary<string, Guid> dic = new Dictionary<string, Guid>();
            string[] parts = isDragDrop ? DdhParts : CmhParts;
            foreach(string part in parts)
            {
                using(RegistryKey cmKey = RegistryEx.GetRegistryKey($@"{shellExPath}\{part}"))
                {
                    if(cmKey == null) continue;
                    foreach(string keyName in cmKey.GetSubKeyNames())
                    {
                        try
                        {
                            using(RegistryKey key = cmKey.OpenSubKey(keyName))
                            {
                                if(!GuidEx.TryParse(key.GetValue("")?.ToString(), out Guid guid))
                                    GuidEx.TryParse(keyName, out guid);
                                if(!guid.Equals(Guid.Empty))
                                    dic.Add(key.Name, guid);
                            }
                        }
                        catch { continue; }
                    }
                }
            }
            return dic;
        }

        public static readonly string[] DdhParts = { "DragDropHandlers", "-DragDropHandlers" };
        public static readonly string[] CmhParts = { "ContextMenuHandlers", "-ContextMenuHandlers" };
        public static readonly Guid LnkOpenGuid = new Guid("00021401-0000-0000-c000-000000000046");

        public ShellExItem(Guid guid, string regPath)
        {
            InitializeComponents();
            this.Guid = guid;
            this.RegPath = regPath;
        }

        private string regPath;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string RegPath
        {
            get => regPath;
            set
            {
                regPath = value;
                this.Text = this.ItemText;
                this.Image = GuidInfo.GetImage(Guid);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Guid Guid { get; set; }
        public string ValueName => null;
        public string SearchText => Text;
        public string ItemFilePath => GuidInfo.GetFilePath(Guid);
        private string KeyName => RegistryEx.GetKeyName(RegPath);
        private string ParentPath => RegistryEx.GetParentPath(RegPath);
        private string ShellExPath => RegistryEx.GetParentPath(ParentPath);
        private string ParentKeyName => RegistryEx.GetKeyName(ParentPath);
        private string DefaultValue => Registry.GetValue(RegPath, "", null)?.ToString();
        public string ItemText => GuidInfo.GetText(Guid) ?? (KeyName.Equals(Guid.ToString("B"), StringComparison.OrdinalIgnoreCase) ? DefaultValue : KeyName);
        public bool IsDragDropItem => ParentKeyName.EndsWith(DdhParts[0], StringComparison.OrdinalIgnoreCase);

        private string BackupPath
        {
            get
            {
                string[] parts = IsDragDropItem ? DdhParts : CmhParts;
                return $@"{ShellExPath}\{(ItemVisible ? parts[1] : parts[0])}\{KeyName}";
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ItemVisible
        {
            get
            {
                string[] parts = IsDragDropItem ? DdhParts : CmhParts;
                return ParentKeyName.Equals(parts[0], StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                try
                {
                    RegistryEx.MoveTo(RegPath, BackupPath);
                }
                catch
                {
                    AppMessageBox.Show(AppString.Message.AuthorityProtection);
                    return;
                }
                RegPath = BackupPath;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MenuButton BtnShowMenu { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public VisibleCheckBox ChkVisible { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DetailedEditButton BtnDetailedEdit { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WebSearchMenuItem TsiSearch { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FileLocationMenuItem TsiFileLocation { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RegLocationMenuItem TsiRegLocation { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RegExportMenuItem TsiRegExport { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public HandleGuidMenuItem TsiHandleGuid { get; set; }

        readonly ToolStripMenuItem TsiDetails = new ToolStripMenuItem(AppString.Menu.Details);

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            ChkVisible = new VisibleCheckBox(this);
            BtnDetailedEdit = new DetailedEditButton(this);
            TsiHandleGuid = new HandleGuidMenuItem(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiRegLocation = new RegLocationMenuItem(this);
            TsiRegExport = new RegExportMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);

            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { TsiHandleGuid, new ToolStripSeparator(),
                TsiDetails, new ToolStripSeparator(), TsiDeleteMe });

            TsiDetails.DropDownItems.AddRange(new ToolStripItem[] { TsiSearch, new ToolStripSeparator(),
                TsiFileProperties, TsiFileLocation, TsiRegLocation, TsiRegExport});

            ContextMenuStrip.Opening += (sender, e) => TsiDeleteMe.Enabled = !(Guid.Equals(LnkOpenGuid) && AppConfig.ProtectOpenItem);
            ChkVisible.PreCheckChanging += TryProtectOpenItem;
        }

        public bool TryProtectOpenItem()
        {
            if(!ChkVisible.Checked || !Guid.Equals(LnkOpenGuid) || !AppConfig.ProtectOpenItem) return true;
            return AppMessageBox.Show(AppString.Message.PromptIsOpenItem, MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        public void DeleteMe()
        {
            RegistryEx.DeleteKeyTree(this.RegPath, true);
            RegistryEx.DeleteKeyTree(this.BackupPath);
        }
    }
}