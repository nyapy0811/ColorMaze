using UnityEngine;

/// <summary>
/// CellGroupLabel의 변형: 라벨을 면 위가 아니라 카메라와 가장 가까운 셀의 정중앙에 놓고,
/// 축에 스냅하지 않고 항상 카메라(플레이어)를 정면으로 바라보도록 회전한다(진짜 빌보드).
/// </summary>
public class CenterFacingLabel : CellGroupLabel
{
    bool queueFixed;

    protected override void LateUpdate()
    {
        if (text == null) return; // Init()이 아직 안 됨

        // 라벨이 큐브 중앙(필터의 반투명 채움 메시 내부)에 놓이기 때문에, 같은 Transparent 큐 안에서
        // 카메라 거리 기준으로 그리기 순서가 매 프레임 조금씩 바뀌며 깜빡이는 문제가 생긴다.
        // 렌더 큐를 채움/테두리 메시보다 뒤로 고정해서 항상 그 위에 그려지게 한다(한 번만 적용).
        if (!queueFixed)
        {
            queueFixed = true;
            var mat = text.fontMaterial; // 접근 시 이 텍스트 전용 인스턴스가 자동 생성됨(공유 애셋 변경 아님)
            if (mat != null) mat.renderQueue += 100;
        }

        var cam = Camera.main;
        if (cam == null) { text.enabled = false; return; }

        Vector3 camPos = cam.transform.position;
        if (!FindNearestCellCenter(camPos, out Vector3 cellCenter))
        {
            text.enabled = false;
            return;
        }

        text.enabled = true;
        transform.position = cellCenter;

        Vector3 toCam = camPos - cellCenter;
        if (toCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(-toCam, Vector3.up);
    }
}
