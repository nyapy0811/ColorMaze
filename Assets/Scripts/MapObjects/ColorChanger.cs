using UnityEngine;

/// <summary>
/// 컬러 체인저(4.5, 보색 필터).
/// 각 색상 스택을 (세 스택 중 최댓값 - 해당 색상의 현재 값)으로 바꾸고, 발동 후 사라진다.
/// currentColorRenderers(현재 플레이어 색)·resultColorRenderers(발동 시 변경될 색)로 미리보기를 보여준다.
/// </summary>
public class ColorChanger : PreviewingStackModifier
{
    [Header("현재 색을 입힐 렌더러 목록")]
    [SerializeField] Renderer[] currentColorRenderers;

    [Header("발동 후 색을 입힐 렌더러 목록")]
    [SerializeField] Renderer[] resultColorRenderers;

    protected override void RefreshPreview()
    {
        if (Player == null) return;

        int max = MaxStack(Player);
        ApplyColorTo(currentColorRenderers, Player.CurrentRGB);
        ApplyColorTo(resultColorRenderers, ColorStacks.ToRGB(
            Transformed(max, Player.Get(LightColor.Red)),
            Transformed(max, Player.Get(LightColor.Green)),
            Transformed(max, Player.Get(LightColor.Blue))));
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
