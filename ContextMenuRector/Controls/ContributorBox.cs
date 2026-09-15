using ContextMenuManager.Helper;
using ContextMenuRector.BluePointLilac.Controls;
using ContextMenuRector.BluePointLilac.Methods;
using ContextMenuRector.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class ContributorBox : FlowLayoutPanel
    {
        public ContributorBox()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.Font = SystemFonts.MenuFont;
            this.Font = new Font(this.Font.FontFamily, this.Font.Size + 1F);
            this.Controls.AddRange([buttonFork, labelThank, panelContent]);
            this.VisibleChanged += (sender, e) => this.SetEnabled(this.Visible);
            labelThank.MouseEnter += (sender, e) => labelThank.ForeColor = Color.FromArgb(0, 162, 255);
            labelThank.MouseLeave += (sender, e) => labelThank.ForeColor = Color.DimGray;
            buttonFork.Click += (sender, e) =>
            {
                // Open GitHub page to fork this project and contribute code
                ExternalProgram.OpenWebUrl(AppConfig.GithubHome);
            };
            buttonFork.Padding = new Padding(16, 6, 0, 0);
            ToolTipBox.SetToolTip(buttonFork, AppString.Dialog.Fork);
            labelHeader.Font = new Font(this.Font, FontStyle.Bold);
            this.OnResize(null);
            this.ResumeLayout();
        }

        readonly PictureButton buttonFork = new PictureButton(AppImage.DownLoad);
        readonly ToolTip toolTip = new ToolTip { InitialDelay = 1 };
        readonly Panel panelContent = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            AutoScroll = true
        };
        readonly Label labelHeader = new Label
        {
            Text = AppString.SideBar.Contributors + "\r\n" + new string('-', 96),
            ForeColor = Color.DarkCyan,
            Dock = DockStyle.Top,
            AutoSize = true
        };
        readonly Label labelThank = new Label
        {
            Font = new Font("Lucida Handwriting", 11F),
            Text = "Welcome to contribute code to this project!",
            ForeColor = Color.DimGray,
            AutoSize = true,
            Padding = new Padding(2, 10, 0, 0)
        };

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int increment = 20.DpiZoom();
            panelContent.Width = this.ClientSize.Width - 2 * increment;
            panelContent.Height = this.ClientSize.Height - panelContent.Top - increment;
        }

        public void LoadLanguages()
        {
            panelContent.SuspendLayout();

            panelContent.Margin = new Padding((Width - panelContent.Width) / 2, 10, 0, 0);

            panelContent.Controls.Remove(labelHeader);
            foreach (Control control in panelContent.Controls) control.Dispose();
            panelContent.Controls.Clear();
            panelContent.Controls.Add(labelHeader);

            SimplePropertyReader reader = new SimplePropertyReader();
            Dictionary<string, string> keyValuePairs = reader.ReadEmbeddedText("ContextMenuManager.Resources.Contributors.ini");

            Dictionary<Label, Label> translatorUiItems = new Dictionary<Label, Label>();
            foreach (KeyValuePair<string, string> pair in keyValuePairs)
            {
                // Key = [Li Guanglin](https://github.com/guanglinn) Value = Current developer
                string[] front = pair.Key.Split("](");
                string name = front[0].TrimStart('[');
                string link = front[1].TrimEnd(')');
                string info = pair.Value;

                Label labelName = new Label
                {
                    AutoSize = true,
                    Font = this.Font,
                    Text = name,
                    ForeColor = Color.DimGray
                };
                if (link.Length > 0)
                {
                    labelName.ForeColor = Color.RoyalBlue;
                    labelName.Font = new Font(labelName.Font, FontStyle.Underline);
                    labelName.Click += (sender, e) => ExternalProgram.OpenWebUrl(link);
                    toolTip.SetToolTip(labelName, link);
                }

                Label labelInfo = new Label()
                {
                    AutoSize = true,
                    Font = this.Font,
                    ForeColor = Color.DimGray,
                    Text = info
                };

                translatorUiItems.Add(labelName, labelInfo);
            }

            int left = 0;
            translatorUiItems.Keys.ToList().ForEach(lbl => left = Math.Max(left, lbl.Width));
            int rightLabelX = left + 100.DpiZoom();
            int top = labelHeader.Bottom + 10.DpiZoom();
            foreach (var uiItem in translatorUiItems)
            {
                // Add name label
                uiItem.Key.Top = top;
                panelContent.Controls.Add(uiItem.Key);

                // Add information label
                Label infoLabel = uiItem.Value;
                infoLabel.Location = new Point(rightLabelX, top);
                panelContent.Controls.Add(infoLabel);

                top += uiItem.Key.Height + 10.DpiZoom();
            }

            panelContent.ResumeLayout();
        }
    }
}
