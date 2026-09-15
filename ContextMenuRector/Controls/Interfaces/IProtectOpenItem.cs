namespace ContextMenuRector.Controls.Interfaces
{
    interface IProtectOpenItem
    {
        bool ItemVisible { get; set; }
        bool TryProtectOpenItem();
    }
}