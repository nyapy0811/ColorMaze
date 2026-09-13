using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>정답 순서1/2 목록의 항목 하나(씬의 OrderList/Viewport/Content/ObjectCard). MapEditController가
/// 런타임에 Instantiate해서 라벨과 위로/아래로/제거 버튼에 코드로 직접 리스너를 연결한다.</summary>
public class OrderListItemUI : MonoBehaviour
{
    public TMP_Text OrderNumberText; // "1", "2", ... — 목록 안에서의 순번
    public TMP_Text ObjectNameText;  // 기물 종류·좌표 라벨
    public Button UpButton;
    public Button DownButton;
    public Button RemoveButton;
}
