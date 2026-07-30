using UnityEngine;

/// <summary>
/// 버킷(4.6).
/// 지정된 색의 스택을 0으로 만들고, 발동 후 사라진다.
/// </summary>
public class Bucket : StackModifierConsumable
{
    [Header("0으로 만들 색")]
    [SerializeField] LightColor targetColor;

    [Header("스택 색을 입힐 렌더러 목록")]
    [SerializeField] Renderer[] stackColorRenderers;

    void Start() => ApplyColor();

    // 인스펙터에서 값을 바꾸면(플레이 전에도) 기본 색이 바로 보이게 한다.
    void OnValidate() => ApplyColor();

    void ApplyColor() => ApplyColorTo(stackColorRenderers, ToColor(targetColor));

    static Color ToColor(LightColor c) => c switch
    {
        LightColor.Red => Color.red,
        LightColor.Green => Color.green,
        _ => Color.blue,
    };

    protected override void ApplyToStacks(ColorStacks player) => player.SetValue(targetColor, 0);
}
