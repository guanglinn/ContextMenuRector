using ContextMenuRector.BluePointLilac.Controls;
using ContextMenuRector.BluePointLilac.Methods;
using ContextMenuRector.Methods;
using System;

namespace ContextMenuRector.Controls
{
    class NewItem : MyListItem
    {
        public NewItem() : this(AppString.Other.NewItem) { }

        public NewItem(string text)
        {
            this.Text = text;
            this.Image = AppImage.NewItem;
            this.AddCtr(BtnAddNewItem);
            ToolTipBox.SetToolTip(BtnAddNewItem, text);
            BtnAddNewItem.MouseDown += (sender, e) => AddNewItem?.Invoke();
            this.MouseDoubleClick += (sender, e) => AddNewItem?.Invoke();

        }
        public Action AddNewItem;
        readonly PictureButton BtnAddNewItem = new PictureButton(AppImage.AddNewItem);
    }
}