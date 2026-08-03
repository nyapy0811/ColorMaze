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
/// 캐릭터의 Red/Green/Blue 스택. 값은 [0, max] 범위(기본 0~15)를 순환한다.
/// 상한을 넘으면 초과한 양만큼 0부터 다시 세고, 0 미만으로 내려가면 초과한 양만큼
/// 상한부터 다시 센다(모듈러 연산). 하한은 항상 0으로 고정이며, max는 세 색상이 항상
/// 공유하는 하나의 값이다(StackMax 상수 한 곳만 바꾸면 전체에 반영됨).
///  - 입력: 다른 스크립트가 Add/Subtract/SetValue를 직접 호출 (예: 맵 기물)
///  - 외부: EventBus의 ColorStackChangeRequest를 구독해 반영
/// 값이 바뀌면 ColorStackChanged를 발행한다.
/// </summary>
public class ColorStacks : MonoBehaviour
{
    /// <summary>모든 색상이 공유하는 스택 상한. 게임 전체에서 이 값 하나만 바꾸면 순환 범위·색 변환이 전부 따라간다.</summary>
    public const int StackMax = 15;

    /// <summary>씬에 하나뿐인 플레이어의 ColorStacks. Awake에서 등록되고 OnDestroy에서 해제된다
    /// (여러 곳에서 FindAnyObjectByType으로 각자 플레이어를 찾던 것을 한 곳으로 모음).</summary>
    public static ColorStacks Instance { get; private set; }

    [Serializable]
    public class Config
    {
        public int start = 0;
    }

    [SerializeField] Config red = new();
    [SerializeField] Config green = new();
    [SerializeField] Config blue = new();

    readonly int[] values = new int[3];

    void Awake()
    {
        Instance = this;
        values[(int)LightColor.Red] = Wrap(red.start, StackMax);
        values[(int)LightColor.Green] = Wrap(green.start, StackMax);
        values[(int)LightColor.Blue] = Wrap(blue.start, StackMax);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

    /// <summary>현재 스택을 변환한 RGB. 채널 = round(255 × 값 ÷ StackMax).</summary>
    public Color32 CurrentRGB => ToRGB(values[0], values[1], values[2]);

    /// <summary>R/G/B 정수 스택을 RGB로 변환한다. 채널마다 독립적으로 [0, StackMax]를 [0, 255]로 매핑한다
    /// (일반 RGB와 동일한 채널별 스케일링, StackMax/StackMax/StackMax가 흰색이 됨). 세 값의 상대적 비율이 아니라
    /// 각 채널의 절대 스택 값이 그대로 밝기에 반영된다.</summary>
    public static Color32 ToRGB(int r, int g, int b)
    {
        return new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(255f * r / StackMax), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(255f * g / StackMax), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(255f * b / StackMax), 0, 255),
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
        int wrapped = Wrap(value, StackMax);
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
        Max = StackMax,
    });
}
