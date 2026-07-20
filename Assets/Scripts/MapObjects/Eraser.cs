using UnityEngine;

/// <summary>
/// 지우개(4.6).
/// 지정된 색의 스택을 0으로 만들고, 발동 후 사라진다.
/// </summary>
[RequireComponent(typeof(FloatingBob))]
public class Eraser : StackModifierConsumable
{
    [Header("0으로 만들 색")]
    [SerializeField] LightColor targetColor;

    [Header("색 순환")]
    [Tooltip("타겟 색과 검정색을 오가는 한 주기(초)")]
    [SerializeField] float cycleDuration = 2f;

    Renderer rend;

    protected override void Awake()
    {
        base.Awake();
        rend = GetComponent<Renderer>();
    }

    // 인스펙터에서 값을 바꾸면(플레이 전에도) 기본 색이 바로 보이게 한다.
    void OnValidate()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        ApplyColor(ToColor(targetColor));
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * 2f * Mathf.PI / cycleDuration) + 1f) * 0.5f;
        ApplyColor(Color.Lerp(Color.black, ToColor(targetColor), t));
    }

    void ApplyColor(Color c)
    {
        if (rend == null) return;
        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);
        rend.SetPropertyBlock(mpb);
    }

    static Color ToColor(LightColor c) => c switch
    {
        LightColor.Red => Color.red,
        LightColor.Green => Color.green,
        _ => Color.blue,
    };

    protected override void ApplyToStacks(ColorStacks player) => player.SetValue(targetColor, 0);
}
