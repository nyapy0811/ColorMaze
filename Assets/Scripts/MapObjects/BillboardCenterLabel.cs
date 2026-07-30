using UnityEngine;

/// <summary>
/// CellGroupLabel의 변형: 텍스트만 갱신하고, 위치·회전은 에디터에서 배치한 그대로 고정한다.
/// 팔레트 자신이 +Y축을 정면으로 플레이어를 바라보도록 회전하므로(ColorPalette 참고),
/// 라벨은 팔레트의 자식으로서 자연스럽게 같이 움직인다 — 라벨 위치를 매 프레임 재계산할 필요가 없다.
/// </summary>
public class BillboardCenterLabel : CellGroupLabel
{
    protected override void LateUpdate()
    {
        if (text != null) text.enabled = true;
    }
}
