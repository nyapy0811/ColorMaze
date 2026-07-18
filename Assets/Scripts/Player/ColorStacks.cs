using System;
using System.Collections.Generic;
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
/// 캐릭터의 Red/Green/Blue 스택. 값은 [0, max] 범위(기본 0~31)를 순환한다.
/// 상한을 넘으면 초과한 양만큼 0부터 다시 세고, 0 미만으로 내려가면 초과한 양만큼
/// 상한부터 다시 센다(모듈러 연산). 하한은 항상 0으로 고정이며, max만 색상별로 다르게
/// 설정해도 범위는 항상 [0, max]가 된다.
///  - 입력: 다른 스크립트가 Add/Subtract/SetValue를 직접 호출 (예: ColorStackInput, 맵 기물)
///  - 외부: EventBus의 ColorStackChangeRequest를 구독해 반영
/// 값이 바뀌면 ColorStackChanged를 발행한다.
/// </summary>
public class ColorStacks : MonoBehaviour
{
    [Serializable]
    public class Config
    {
        public int start = 0;
        public int max = 31;
    }

    [SerializeField] Config red = new();
    [SerializeField] Config green = new();
    [SerializeField] Config blue = new();

    readonly int[] values = new int[3];

    void Awake()
    {
        values[(int)LightColor.Red] = Wrap(red.start, red.max);
        values[(int)LightColor.Green] = Wrap(green.start, green.max);
        values[(int)LightColor.Blue] = Wrap(blue.start, blue.max);
    }

    /// <summary>value를 [0, max] 범위로 순환(모듈러)시킨다.</summary>
    static int Wrap(int value, int max)
    {
        int width = max + 1;
        int v = value % width;
        if (v < 0) v += width;
        return v;
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

    /// <summary>값을 증감한다(음수 delta 가능). 결과는 순환 범위를 따른다.</summary>
    public void Add(LightColor c, int amount) => SetValue(c, values[(int)c] + amount);

    public void Subtract(LightColor c, int amount) => Add(c, -amount);

    /// <summary>절대값을 지정한다(순환 범위로 Wrap). 변하면 이벤트 발행.</summary>
    public void SetValue(LightColor c, int value)
    {
        int wrapped = Wrap(value, ConfigOf(c).max);
        if (wrapped == values[(int)c]) return;
        values[(int)c] = wrapped;
        Publish(c);
    }

    /// <summary>세 스택 중 최댓값을 가진 색상 목록을 반환한다(동률이면 여러 개).</summary>
    public List<LightColor> GetMaxColors()
    {
        int max = Mathf.Max(values[0], Mathf.Max(values[1], values[2]));
        var result = new List<LightColor>(3);
        for (int i = 0; i < 3; i++)
            if (values[i] == max) result.Add((LightColor)i);
        return result;
    }

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
