using UnityEngine;

/// <summary>
/// RGB 필터(4.3, 테두리가 RGB 색).
/// 컬러 필터와 달리 정확한 색 일치가 아니라, 플레이어의 최댓값 채널 목록(동률 포함)에
/// 이 필터가 요구하는 색이 포함되는지만 판정한다.
/// 외형은 지정 채널의 순수 원색(빨강/초록/파랑)을 사용하며, FilterBlockBase를 통해
/// 컬러 필터와 동일한 방식으로 메시 병합 대상이 된다(같은 색끼리 하나의 메시로 합쳐짐).
/// </summary>
public class RgbFilterBlock : FilterBlockBase
{
    [Header("이 필터가 요구하는 색상")]
    [SerializeField] LightColor targetColor;

    protected override bool Matches(ColorStacks player) => player.GetMaxColors().Contains(targetColor);

    protected override Color32 GetAppearanceColor() => targetColor switch
    {
        LightColor.Red => new Color32(255, 0, 0, 255),
        LightColor.Green => new Color32(0, 255, 0, 255),
        _ => new Color32(0, 0, 255, 255),
    };

    // 텍스트 라벨 없이 테두리 색(지정 원색)만으로 종류를 구분한다.
    protected override string GetLabelText() => null;
}
