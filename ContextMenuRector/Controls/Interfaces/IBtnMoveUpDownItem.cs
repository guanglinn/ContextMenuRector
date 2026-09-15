using ContextMenuRector.BluePointLilac.Controls;
using ContextMenuRector.Methods;

namespace ContextMenuRector.Controls.Interfaces
{
    interface IBtnMoveUpDownItem
    {
        MoveButton BtnMoveUp { get; set; }
        MoveButton BtnMoveDown { get; set; }
    }

    sealed class MoveButton : PictureButton
    {
        public MoveButton(IBtnMoveUpDownItem item, bool isUp) : base(isUp ? AppImage.Up : AppImage.Down)
        {
            ((MyListItem)item).AddCtr(this);
        }
    }
}