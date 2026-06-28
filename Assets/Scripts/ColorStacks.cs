using System;
using Framework.Core;
using UnityEngine;

/// <summary>빛의 삼원색 스택 종류.</summary>
public enum LightColor { Red, Green, Blue }

/// <summary>스택 값이 바뀌면 발행된다(HUD 등이 구독해 표시).</summary>
public struct ColorStackChanged : IEvent
{
    public LightColor Color;
    public int Value;
    public int Max;
}

/// <summary>외부(트랩·아이템 등)에서 값 변경을 요청할 때 발행한다.</summary>
public struct ColorStackChangeRequest : IEvent
{
    public LightColor Color;
    public int Delta;
}

/// <summary>
/// 캐릭터의 Red/Green/Blue 스택. 값은 0 이상의 정수이며 각 상한에서 클램프된다.
///  - 입력: 다른 스크립트가 Add/Subtract를 직접 호출 (예: ColorStackInput)
///  - 외부: EventBus의 ColorStackChangeRequest를 구독해 반영
/// 값이 바뀌면 ColorStackChanged를 발행한다.
/// </summary>
public class ColorStacks : MonoBehaviour
{
    [Serializable]
    public class Config
    {
        [Min(0)] public int start = 0;
        [Min(0)] public int max = 10;
    }

    [SerializeField] Config red = new();
    [SerializeField] Config green = new();
    [SerializeField] Config blue = new();

    readonly int[] values = new int[3];

    void Awake()
    {
        values[(int)LightColor.Red] = Mathf.Clamp(red.start, 0, red.max);
        values[(int)LightColor.Green] = Mathf.Clamp(green.start, 0, green.max);
        values[(int)LightColor.Blue] = Mathf.Clamp(blue.start, 0, blue.max);
    }

    void OnEnable() => EventBus.Subscribe<ColorStackChangeRequest>(OnChangeRequest);
    void OnDisable() => EventBus.Unsubscribe<ColorStackChangeRequest>(OnChangeRequest);

    void Start()
    {
        for (int i = 0; i < 3; i++) Publish((LightColor)i); // 초기값 1회 알림
    }

    public int Get(LightColor c) => values[(int)c];
    public int Max(LightColor c) => ConfigOf(c).max;

    /// <summary>현재 스택을 변환한 RGB. 채널 = round(255 × 값 ÷ 세 값 중 최댓값).</summary>
    public Color32 CurrentRGB => ToRGB(values[0], values[1], values[2]);

    /// <summary>R/G/B 정수 스택을 RGB로 변환한다(가장 큰 값이 255가 되도록 정규화, 반올림).</summary>
    public static Color32 ToRGB(int r, int g, int b)
    {
        int max = Mathf.Max(r, Mathf.Max(g, b));
        if (max <= 0) return new Color32(0, 0, 0, 255);
        return new Color32(
            (byte)Mathf.RoundToInt(255f * r / max),
            (byte)Mathf.RoundToInt(255f * g / max),
            (byte)Mathf.RoundToInt(255f * b / max),
            255);
    }

    /// <summary>모든 스택을 0으로 초기화한다.</summary>
    public void ResetAll()
    {
        for (int i = 0; i < 3; i++)
            if (values[i] != 0) { values[i] = 0; Publish((LightColor)i); }
    }

    /// <summary>값을 증감한다(음수 가능). [0, max]로 클램프되고 변하면 이벤트 발행.</summary>
    public void Add(LightColor c, int amount)
    {
        int clamped = Mathf.Clamp(values[(int)c] + amount, 0, ConfigOf(c).max);
        if (clamped == values[(int)c]) return;
        values[(int)c] = clamped;
        Publish(c);
    }

    public void Subtract(LightColor c, int amount) => Add(c, -amount);

    void OnChangeRequest(ColorStackChangeRequest e) => Add(e.Color, e.Delta);

    void Publish(LightColor c) => EventBus.Publish(new ColorStackChanged
    {
        Color = c,
        Value = values[(int)c],
        Max = ConfigOf(c).max,
    });

    Config ConfigOf(LightColor c) => c switch
    {
        LightColor.Red => red,
        LightColor.Green => green,
        _ => blue,
    };
}
