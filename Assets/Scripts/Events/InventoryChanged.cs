using Framework.Core;

namespace ColorMaze
{
    /// <summary>소지 오브가 바뀜(더함, 뺌, 불러옴). 받는 쪽이 소지 오브를 직접 읽는다. 보내는 곳: 소지 오브 컴포넌트.</summary>
    public readonly struct InventoryChanged : IEvent
    {
        public readonly PlayerInventory Inventory;

        public InventoryChanged(PlayerInventory inventory) => Inventory = inventory;
    }
}
