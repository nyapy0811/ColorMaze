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

    // 인스펙터에서 값을 바꾸면 에디터에서도 바로 반영되게 한다.
    void OnValidate() => ApplyTargetColor();

    void Start() => ApplyTargetColor();

    // 첫 번째 자식의 렌더러 색을 목표 스택 값이 나타내는 색으로 표시한다.
    void ApplyTargetColor()
    {
        if (transform.childCount == 0) return;
        if (!transform.GetChild(0).TryGetComponent<Renderer>(out var r)) return;

        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", ColorStacks.ToRGB(targetRed, targetGreen, targetBlue));
        r.SetPropertyBlock(mpb);
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
