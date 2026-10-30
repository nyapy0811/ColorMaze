using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 제출 화면(그림 3·4). 왼쪽 위에 요구 값(세 칸), 오른쪽 위에 제출 슬롯(빈 원), 아래에 소지 오브 목록.
    /// 소지 오브를 누르면 낸다: 받아들이면 슬롯으로 옮겨지고, 틀리거나 슬롯이 차 있으면 그 오브가 제자리에서 흔들린다.
    /// 슬롯 오브를 누르면 되돌리거나 회수하고, 소지 오브가 가득 차 있으면 흔들린다. 판정은 세션이 하고 결과는 닫을 때 작동기가 적용한다.
    /// </summary>
    public class SubmitPopup : PopupView
    {
        [SerializeField] OrbValueLabel requiredLabel;

        [Tooltip("제출 슬롯에 든 오브. 슬롯이 비면 숨기고 뒤의 빈 원만 보인다.")]
        [SerializeField] OrbCellView slotCell;

        [Tooltip("소지 오브 목록")]
        [SerializeField] RectTransform inventoryRow;

        [SerializeField] OrbCellView cellPrefab;

        SubmitSession session;

        public override bool Accepts(object popupSession) => popupSession is SubmitSession;

        public override void Show(object popupSession)
        {
            session = (SubmitSession)popupSession;
            requiredLabel.Show(session.Required);
            Redraw();
        }

        public override void Hide() => session = null;

        void Redraw()
        {
            Clear(inventoryRow);
            foreach (var orb in session.Orbs)
            {
                var cell = Instantiate(cellPrefab, inventoryRow);
                cell.Set(orb.Value, false, () => Submit(orb, cell));
            }

            slotCell.gameObject.SetActive(session.SlotOrb.HasValue);
            if (session.SlotOrb.HasValue) slotCell.Set(session.SlotOrb.Value, false, Retrieve);
        }

        void Submit(HeldOrb orb, OrbCellView cell)
        {
            if (session.TrySubmit(orb, out _)) Redraw();
            else cell.Shake();
        }

        void Retrieve()
        {
            if (session.CanRetrieve && session.TryRetrieve(out _)) Redraw();
            else slotCell.Shake();
        }
    }
}
