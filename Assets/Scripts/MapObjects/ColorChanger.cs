using Framework.Core;
using UnityEngine;

/// <summary>
/// 컬러 체인저(4.5, 보색 필터).
/// 각 색상 스택을 (세 스택 중 최댓값 - 해당 색상의 현재 값)으로 바꾸고, 발동 후 사라진다.
/// 자식 구 2개(0번째 = 현재 플레이어 색, 1번째 = 발동 시 변경될 색)로 미리보기를 보여준다.
/// 미리보기는 매 프레임이 아니라 스테이지 시작 시와 플레이어 스택이 바뀔 때만 갱신한다.
/// </summary>
public class ColorChanger : SpinningStackModifier
{
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

        int max = MaxStack(Player);
        ApplyChildColor(0, Player.CurrentRGB);
        ApplyChildColor(1, ColorStacks.ToRGB(
            Transformed(max, Player.Get(LightColor.Red)),
            Transformed(max, Player.Get(LightColor.Green)),
            Transformed(max, Player.Get(LightColor.Blue))));
    }

    void ApplyChildColor(int childIndex, Color32 color)
    {
        if (childIndex >= transform.childCount) return;
        if (!transform.GetChild(childIndex).TryGetComponent<Renderer>(out var r)) return;

        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", (Color)color);
        r.SetPropertyBlock(mpb);
    }

    static int MaxStack(ColorStacks player) => Mathf.Max(player.Get(LightColor.Red),
        Mathf.Max(player.Get(LightColor.Green), player.Get(LightColor.Blue)));

    static int Transformed(int max, int current) => max - current;

    protected override void ApplyToStacks(ColorStacks player)
    {
        int max = MaxStack(player);
        foreach (LightColor c in System.Enum.GetValues(typeof(LightColor)))
            player.SetValue(c, Transformed(max, player.Get(c)));
    }
}
