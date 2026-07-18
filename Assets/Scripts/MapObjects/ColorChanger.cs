using UnityEngine;

/// <summary>
/// 컬러 체인저(4.5, 보색 필터).
/// 각 색상 스택을 (세 스택 중 최댓값 - 해당 색상의 현재 값)으로 바꾸고, 발동 후 사라진다.
/// </summary>
public class ColorChanger : StackModifierConsumable
{
    protected override void ApplyToStacks(ColorStacks player)
    {
        int max = Mathf.Max(player.Get(LightColor.Red),
                   Mathf.Max(player.Get(LightColor.Green), player.Get(LightColor.Blue)));

        foreach (LightColor c in System.Enum.GetValues(typeof(LightColor)))
            player.SetValue(c, max - player.Get(c));
    }
}
