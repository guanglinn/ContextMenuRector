using ContextMenuRector.BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuRector.BluePointLilac.Controls
{
    public sealed class MyToolBar : FlowLayoutPanel
    {
        public MyToolBar()
        {
            this.Height = 80.DpiZoom();
            this.Dock = DockStyle.Top;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(85, 145, 215);
        }

        private MyToolBarButton selectedButton;
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MyToolBarButton SelectedButton
        {
            get => selectedButton;
            set
            {
                if (selectedButton == value) return;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = 0;
                    selectedButton.Cursor = Cursors.Hand;
                }
                selectedButton = value;
                if (selectedButton != null)
                {
                    selectedButton.Opacity = 0.4F;
                    selectedButton.Cursor = Cursors.Default;
                }
                SelectedButtonChanged?.Invoke(this, null);
            }
        }

        public event EventHandler SelectedButtonChanged;

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int SelectedIndex
        {
            get
            {
                if (SelectedButton == null) return -1;
                else return Controls.GetChildIndex(SelectedButton);
            }
            set
            {
                if (value < 0 || value >= this.Controls.Count) SelectedButton = null;
                else SelectedButton = (MyToolBarButton)Controls[value];
            }
        }

        public void AddButton(MyToolBarButton button)
        {
            this.SuspendLayout();
            button.Parent = this;
            button.Margin = new Padding(12, 4, 0, 0).DpiZoom();
            button.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left && button.CanBeSelected) SelectedButton = button;
            };
            button.MouseEnter += (sender, e) =>
            {
                if (button != SelectedButton) button.Opacity = 0.2F;
            };
            button.MouseLeave += (sender, e) =>
            {
                if (button != SelectedButton) button.Opacity = 0;
            };
            this.ResumeLayout();
        }

        public void AddButtons(MyToolBarButton[] buttons)
        {
            int maxWidth = 72.DpiZoom();
            Array.ForEach(buttons, button => maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(button.Text, button.Font).Width));
            Array.ForEach(buttons, button => { button.Width = maxWidth; AddButton(button); });
        }

        public void AddSearchBox(MyToolBarSearchBox searchBox)
        {
            this.SuspendLayout();
            searchBox.Parent = this;
            searchBox.Margin = new Padding(12, 4, 0, 0).DpiZoom();
            this.ResumeLayout();
        }
    }

    public sealed class MyToolBarButton : Panel
    {
        public MyToolBarButton(Image image, string text)
        {
            this.SuspendLayout();
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(72, 72).DpiZoom();
            this.Controls.AddRange(new Control[] { picImage, lblText });
            lblText.Resize += (sender, e) => this.OnResize(null);
            picImage.Top = 6.DpiZoom();
            lblText.Top = 52.DpiZoom();
            lblText.SetEnabled(false);
            this.Image = image;
            this.Text = text;
            this.ResumeLayout();
        }

        readonly PictureBox picImage = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(40, 40).DpiZoom(),
            BackColor = Color.Transparent,
            Enabled = false
        };

        readonly Label lblText = new Label
        {
            BackColor = Color.Transparent,
            Font = SystemFonts.MenuFont,
            ForeColor = Color.White,
            AutoSize = true,
        };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image Image
        {
            get => picImage.Image;
            set => picImage.Image = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new string Text
        {
            get => lblText.Text;
            set => lblText.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Opacity
        {
            get => BackColor.A / 255;
            set => BackColor = Color.FromArgb((int)(value * 255), Color.White);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CanBeSelected { get; set; } = true;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            lblText.Left = (this.Width - lblText.Width) / 2;
            picImage.Left = (this.Width - picImage.Width) / 2;
        }
    }

    public sealed class MyToolBarSearchBox : Panel
    {
        private readonly TextBox textBox;
        private readonly Label label;

        public MyToolBarSearchBox(EventHandler filter)
        {
            this.SuspendLayout();
            this.BackColor = Color.Transparent;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0, 0, 0, 18);
            (textBox, label) = inflate();
            textBox.TextChanged += (sender, e) => filter?.Invoke(sender, e);
            textBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter) filter?.Invoke(sender, e);
            };
            label.Click += (sender, e) => { filter?.Invoke(textBox, e); };
            this.ResumeLayout();
        }

        private (TextBox textBox, Label label) inflate()
        {
            TextBox textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 16, Font.Style)
            };

            Panel parent = new Panel
            {
                BackColor = textBox.BackColor,
                ForeColor = Color.FromArgb(80, 80, 80),
                Dock = DockStyle.Bottom,
                Padding = new Padding(6, 4, 6, 4)
            };
            parent.Height = textBox.Height + parent.Padding.Top + parent.Padding.Bottom;

            Label label = new Label
            {
                Text = "🔍",
                Dock = DockStyle.Right,
                BackColor = Color.LightGreen,
                Padding = new Padding(4, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                Width = 30.DpiZoom()
            };

            Panel root = new Panel
            {
                BackColor = Color.Transparent,
                Dock = DockStyle.Bottom,
                Height = parent.Height
            };

            parent.Controls.Add(textBox);
            root.Controls.Add(parent);
            root.Controls.Add(label);
            this.Controls.Add(root);

            return (textBox, label);
        }

        public TextBox TextBox
        {
            get => textBox;
        }
    }
}
