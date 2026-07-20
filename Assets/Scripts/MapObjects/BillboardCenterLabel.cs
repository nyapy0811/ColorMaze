using UnityEngine;

/// <summary>
/// CellGroupLabel의 변형: 오브젝트 중심에 고정되고, 항상 플레이어(카메라)를 향해 회전하는 라벨.
/// 필터처럼 면에 붙어 고정되는 CellGroupLabel과 달리, 컬러 팔레트처럼 항상 플레이어를
/// 바라봐야 하는 기물에 쓴다. 셀은 1개만 등록해서 쓰는 게 자연스럽다(첫 번째 셀만 사용).
/// </summary>
public class BillboardCenterLabel : CellGroupLabel
{
    // 중심에서 카메라 쪽 표면 밖으로 밀어내는 거리(자기 자신의 메시에 가려지지 않도록).
    // 컬러 팔레트는 지름 0.5짜리 구라서 반지름(0.25)보다 살짝 크게 잡는다.
    const float CameraOffset = 0.3f;

    protected override void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || transform.parent == null)
        {
            text.enabled = false;
            return;
        }

        // 부모(팔레트) 오브젝트의 실제 위치를 따라간다(FloatingBob 등으로 움직여도 같이 움직임).
        Vector3 center = transform.parent.position;
        Vector3 camPos = cam.transform.position;

        // 카메라 반대 방향(뷰어에서 멀어지는 쪽)을 forward로 둬야 텍스트 좌우가 뒤집히지 않는다.
        Vector3 dir = center - camPos;
        Vector3 up = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;

        text.enabled = true;
        transform.position = center - dir.normalized * CameraOffset;
        transform.rotation = Quaternion.LookRotation(dir, up);
    }
}
