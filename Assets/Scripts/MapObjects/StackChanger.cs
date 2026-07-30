using Framework.Core;
using UnityEngine;

/// <summary>
/// 스택 체인저(4.4).
/// 지정된 두 색의 스택 값을 서로 교환하고, 발동 후 사라진다.
/// currentColorRenderers(현재 플레이어 색)·resultColorRenderers(발동 시 변경될 색)로 미리보기를 보여준다.
/// 미리보기는 매 프레임이 아니라 스테이지 시작 시와 플레이어 스택이 바뀔 때만 갱신한다.
/// </summary>
public class StackChanger : StackModifierConsumable
{
    [Header("교환할 두 색")]
    [SerializeField] LightColor colorA = LightColor.Red;
    [SerializeField] LightColor colorB = LightColor.Blue;

    [Header("현재 색을 입힐 렌더러 목록")]
    [SerializeField] Renderer[] currentColorRenderers;

    [Header("발동 후 색을 입힐 렌더러 목록")]
    [SerializeField] Renderer[] resultColorRenderers;

    void OnEnable()
    {
        EventBus.Subscribe<ColorStackChanged>(OnStackChanged);
        EventBus.Subscribe<SceneLoadCompleted>(OnStageStart);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<ColorStackChanged>(OnStackChanged);
        EventBus.Unsubscribe<SceneLoadCompleted>(OnStageStart);
    }

    void Start() => RefreshPreview();

    void OnStackChanged(ColorStackChanged e) => RefreshPreview();
    void OnStageStart(SceneLoadCompleted e) => RefreshPreview();

    void RefreshPreview()
    {
        if (Player == null) return;

        ApplyColorTo(currentColorRenderers, Player.CurrentRGB);

        int r = Player.Get(LightColor.Red);
        int g = Player.Get(LightColor.Green);
        int b = Player.Get(LightColor.Blue);
        int a = Player.Get(colorA);
        int bVal = Player.Get(colorB);
        Set(ref r, ref g, ref b, colorA, bVal);
        Set(ref r, ref g, ref b, colorB, a);

        ApplyColorTo(resultColorRenderers, ColorStacks.ToRGB(r, g, b));
    }

    static void Set(ref int r, ref int g, ref int b, LightColor c, int value)
    {
        switch (c)
        {
            case LightColor.Red: r = value; break;
            case LightColor.Green: g = value; break;
            default: b = value; break;
        }
    }

    protected override void ApplyToStacks(ColorStacks player)
    {
        int a = player.Get(colorA);
        int b = player.Get(colorB);
        player.SetValue(colorA, b);
        player.SetValue(colorB, a);
    }
}
