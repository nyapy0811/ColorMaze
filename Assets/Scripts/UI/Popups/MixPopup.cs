using TMPro;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 합성 화면(그림 2). 위 칸은 고른 오브의 'A + B → 결과'이고 결과를 누르면 합성한다. 아래 칸은 소지 오브 목록이고
    /// 누르면 고르기·취소다(고른 오브는 굵은 테두리, 위 칸의 오브를 눌러도 취소). 합성 세션의 선택·미리보기를 그대로 그리며,
    /// 합성한 뒤에도 열린 채로 남는다(3차 Q480). 소지 오브가 없어도 열린다.
    /// </summary>
    public class MixPopup : PopupView
    {
        [Tooltip("위 칸: A + B → 결과")]
        [SerializeField] RectTransform formulaRow;

        [Tooltip("아래 칸: 소지 오브 목록")]
        [SerializeField] RectTransform inventoryRow;

        [SerializeField] OrbCellView cellPrefab;

        [Tooltip("'+'와 '→'를 쓰는 글자")]
        [SerializeField] TMP_Text symbolPrefab;

        MixSession session;

        public override bool Accepts(object popupSession) => popupSession is MixSession;

        public override void Show(object popupSession)
        {
            session = (MixSession)popupSession;
            Redraw();
        }

        public override void Hide() => session = null;

        void Redraw()
        {
            Clear(formulaRow);
            Clear(inventoryRow);

            foreach (var orb in session.Orbs)
                AddCell(inventoryRow).Set(orb.Value, session.IsSelected(orb), () => Toggle(orb));

            var selected = session.Selected;
            for (int i = 0; i < selected.Count; i++)
            {
                if (i > 0) AddSymbol("+");
                var orb = selected[i];
                AddCell(formulaRow).Set(orb.Value, false, () => Toggle(orb));
            }
            if (session.CanMix)
            {
                AddSymbol("→");
                AddCell(formulaRow).Set(session.Preview, false, Mix);
            }
        }

        void Toggle(HeldOrb orb)
        {
            session.Toggle(orb);
            Redraw();
        }

        void Mix()
        {
            session.Mix();
            Redraw();
        }

        OrbCellView AddCell(RectTransform row) => Instantiate(cellPrefab, row);

        void AddSymbol(string symbol) => Instantiate(symbolPrefab, formulaRow).text = symbol;
    }
}
