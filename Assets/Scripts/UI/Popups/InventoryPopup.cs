using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 인벤토리 창(7.7, Tab). 소지 오브를 슬롯 수만큼의 칸에 들어온 순서로 그리고(빈 슬롯은 빈 원), 값을 늘 보이며,
    /// 아래에 지금 채널 한도를 보인다. 지금은 소지 오브를 그대로 받아 보기만 한다(흡수 칸과 흡수 세션은 코어 루프 뒤).
    /// 열린 동안은 입력이 잠겨 소지 오브가 바뀌지 않으므로 열 때 한 번 그린다.
    /// </summary>
    public class InventoryPopup : PopupView
    {
        [Tooltip("슬롯 줄")]
        [SerializeField] RectTransform slotRow;

        [SerializeField] OrbCellView cellPrefab;

        [Tooltip("빈 슬롯(빈 원)")]
        [SerializeField] GameObject emptySlotPrefab;

        [SerializeField] OrbValueLabel channelLimitLabel;

        public override bool Accepts(object popupSession) => popupSession is OrbInventory;

        public override void Show(object popupSession)
        {
            var inventory = (OrbInventory)popupSession;
            Clear(slotRow);
            foreach (var orb in inventory.Items)
            {
                var cell = Instantiate(cellPrefab, slotRow);
                cell.Set(orb.Value, false, null);
                cell.PinValue();
            }
            for (int i = inventory.Count; i < inventory.Capacity; i++) Instantiate(emptySlotPrefab, slotRow);
            channelLimitLabel.Show(inventory.ChannelLimit);
        }
    }
}
