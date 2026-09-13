using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>CanvasList의 캔버스 카드 하나(씬의 CorrectOrder/CanvasList/Viewport/Content/CanvasCard).
/// 클릭하면 그 캔버스의 정답 순서를 OrderList에 표시한다.</summary>
public class CanvasCardUI : MonoBehaviour
{
    public TMP_Text NumberText;
    public Button SelectButton;
    public Image CardImage; // 선택 상태 하이라이트용
}
