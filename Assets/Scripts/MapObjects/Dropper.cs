using UnityEngine;

/// <summary>
/// 스포이드(4.6).
/// 지정된 색의 스택을 0으로 만들고, 발동 후 사라진다.
/// </summary>
public class Dropper : StackModifierConsumable
{
    [Header("0으로 만들 색")]
    [SerializeField] LightColor targetColor;

    protected override void ApplyToStacks(ColorStacks player) => player.SetValue(targetColor, 0);
}
