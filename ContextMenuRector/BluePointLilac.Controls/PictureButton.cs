using System;
using System.Drawing;
using System.Windows.Forms;

namespace ContextMenuRector.BluePointLilac.Controls
{
    public class PictureButton : PictureBox
    {
        public PictureButton(Image image)
        {
            this.BaseImage = image;
            this.SizeMode = PictureBoxSizeMode.AutoSize;
            this.Cursor = Cursors.Hand;
        }

        private Image baseImage;

        // 标记为 Hidden，设计器不会生成 this.myControl.IsSelected = true; 这样的代码
        [Browsable(false)] // 通常配合 Browsable(false) 一起使用，使其也不在属性窗口显示
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image BaseImage
        {
            get => baseImage;
            set
            {
                baseImage = value;
                this.Image = ToolStripRenderer.CreateDisabledImage(value);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e); this.Image = BaseImage;
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.Image = ToolStripRenderer.CreateDisabledImage(BaseImage);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) base.OnMouseDown(e);
        }
    }
}