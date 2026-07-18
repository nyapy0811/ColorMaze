using UnityEngine;

/// <summary>
/// 스택 체인저(4.4).
/// 지정된 두 색의 스택 값을 서로 교환하고, 발동 후 사라진다.
/// </summary>
public class StackChanger : StackModifierConsumable
{
    [Header("교환할 두 색")]
    [SerializeField] LightColor colorA = LightColor.Red;
    [SerializeField] LightColor colorB = LightColor.Blue;

    protected override void ApplyToStacks(ColorStacks player)
    {
        int a = player.Get(colorA);
        int b = player.Get(colorB);
        player.SetValue(colorA, b);
        player.SetValue(colorB, a);
    }
}
