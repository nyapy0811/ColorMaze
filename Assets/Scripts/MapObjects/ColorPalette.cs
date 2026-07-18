using UnityEngine;

/// <summary>
/// 컬러 팔레트(4.1).
/// 충돌하면 지정된 만큼 RGB 스택이 증가한다. 사라지지 않아 반복 획득할 수 있다.
/// </summary>
public class ColorPalette : AcquireObjectBase
{
    [Header("증가시킬 스택량")]
    [SerializeField] int red;
    [SerializeField] int green;
    [SerializeField] int blue;

    protected override void OnAcquire(ColorStacks player)
    {
        if (red != 0) player.Add(LightColor.Red, red);
        if (green != 0) player.Add(LightColor.Green, green);
        if (blue != 0) player.Add(LightColor.Blue, blue);
    }
}
