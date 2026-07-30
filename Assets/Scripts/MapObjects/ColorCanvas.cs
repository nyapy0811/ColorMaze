using TMPro;
using UnityEngine;

/// <summary>
/// 캔버스(4.7).
/// 플레이어의 RGB 스택 값이 지정된 값과 정확히 일치하면 완료(잠금)된다.
/// (필터류와 달리 정규화된 색이 아니라 스택 값 자체를 비교한다.)
/// </summary>
public class ColorCanvas : ClearObjectBase
{
    [Header("목표 스택 값")]
    [SerializeField] int targetRed;
    [SerializeField] int targetGreen;
    [SerializeField] int targetBlue;

    [Header("스택 색을 입힐 렌더러 목록")]
    [SerializeField] Renderer[] stackColorRenderers;

    // 인스펙터에서 값을 바꾸면 에디터에서도 바로 반영되게 한다.
    void OnValidate()
    {
        ApplyTargetColor();
        ApplyTargetLabel();
    }

    void Start()
    {
        ApplyTargetColor();
        ApplyTargetLabel();
    }

    // stackColorRenderers 전체를 목표 스택 값이 나타내는 색으로 표시한다.
    void ApplyTargetColor() => ApplyColorTo(stackColorRenderers, ColorStacks.ToRGB(targetRed, targetGreen, targetBlue));

    // 첫 번째 자식의 -Z면(고정, 카메라를 따라 돌지 않음)에 목표 R/G/B 값을 표시한다.
    // 라벨 자동 생성은 필터만 하므로(FilterBlockBase), 캔버스는 자식으로 이미 있는 라벨을 찾아서 쓴다.
    void ApplyTargetLabel()
    {
        if (transform.childCount == 0) return;
        Transform child = transform.GetChild(0);

        TextMeshPro targetLabel = child.GetComponentInChildren<TextMeshPro>();
        if (targetLabel == null) return; // 라벨이 없으면 아무것도 안 함(자동 생성하지 않음)

        // 필터 라벨(CellGroupLabel)과 동일한 공식: normal 방향으로 살짝 띄우고, 뒤집히지 않게 -normal을 forward로.
        Vector3 normal = child.TransformDirection(Vector3.back);
        Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;

        float zOffset = child.lossyScale.z * 0.5f + 0.02f;
        targetLabel.transform.position = child.position + normal * zOffset;
        targetLabel.transform.rotation = Quaternion.LookRotation(-normal, up);

        // 캔버스는 정숫값 자체로 판정하므로 공백 구분 형식을 쓴다.
        targetLabel.text = StackLabelFormat.ByValue(targetRed, targetGreen, targetBlue);
    }

    protected override bool CheckCondition(ColorStacks player) =>
        player.Get(LightColor.Red) == targetRed &&
        player.Get(LightColor.Green) == targetGreen &&
        player.Get(LightColor.Blue) == targetBlue;

    protected override void OnCompleted()
    {
        // TODO: 완료 연출(발광 등)은 아트 연동 시 추가. 지금은 확인용 로그만 남긴다.
        Debug.Log($"[ColorCanvas] {name} 완료");
    }
}
