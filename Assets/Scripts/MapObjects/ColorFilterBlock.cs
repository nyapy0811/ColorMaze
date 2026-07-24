using UnityEngine;

/// <summary>
/// 색상 통과 블록(4.2 컬러 필터).
/// 블록별로 R/G/B 스택을 지정하고, 같은 변환식으로 RGB를 만든다.
/// 플레이어의 변환 RGB가 이 필터의 RGB와 정확히 같을 때만 통과 가능(그 외엔 솔리드로 막음).
/// 통과·초기화·외형·메시 병합 로직은 FilterBlockBase가 담당한다.
/// </summary>
public class ColorFilterBlock : FilterBlockBase
{
    [Header("이 필터가 요구하는 스택")]
    [SerializeField, Min(0)] int red;
    [SerializeField, Min(0)] int green;
    [SerializeField, Min(0)] int blue;

    protected override Color32 GetAppearanceColor() => ColorStacks.ToRGB(red, green, blue);

    protected override bool Matches(ColorStacks player) => ColorEquals(player.CurrentRGB, GetAppearanceColor());

    // 테두리(지정 스택을 색으로 표현)와 함께, 각 색의 정확한 스택 값을 해당 색으로 물들인 숫자로 보여준다.
    // 필터는 변환 색(비율)로 판정하므로 ":" 구분 형식을 쓴다.
    protected override string GetLabelText() => StackLabelFormat.ByRatio(red, green, blue);

    static bool ColorEquals(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;
}
