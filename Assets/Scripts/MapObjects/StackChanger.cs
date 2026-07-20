using UnityEngine;

/// <summary>
/// 스택 체인저(4.4).
/// 지정된 두 색의 스택 값을 서로 교환하고, 발동 후 사라진다.
/// </summary>
[RequireComponent(typeof(FloatingBob))]
public class StackChanger : SpinningStackModifier
{
    [Header("교환할 두 색")]
    [SerializeField] LightColor colorA = LightColor.Red;
    [SerializeField] LightColor colorB = LightColor.Blue;

    // 자식으로 붙은 작은 구 2개(0번째 = colorA, 1번째 = colorB)에 각 색을 표시한다.
    void Start() => ApplyChildColors();

    // 인스펙터에서 값을 바꾸면 에디터에서도 바로 반영되게 한다.
    void OnValidate() => ApplyChildColors();

    void ApplyChildColors()
    {
        ApplyChildColor(0, colorA);
        ApplyChildColor(1, colorB);
    }

    void ApplyChildColor(int childIndex, LightColor color)
    {
        if (childIndex >= transform.childCount) return;
        if (!transform.GetChild(childIndex).TryGetComponent<Renderer>(out var r)) return;

        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", ToColor(color));
        r.SetPropertyBlock(mpb);
    }

    static Color ToColor(LightColor c) => c switch
    {
        LightColor.Red => Color.red,
        LightColor.Green => Color.green,
        _ => Color.blue,
    };

    protected override void ApplyToStacks(ColorStacks player)
    {
        int a = player.Get(colorA);
        int b = player.Get(colorB);
        player.SetValue(colorA, b);
        player.SetValue(colorB, a);
    }
}
