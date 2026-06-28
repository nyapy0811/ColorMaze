using Framework.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD에 Red/Green/Blue 스택을 표시한다.
///  - 색상 출력: 변환 정수(0~255)를 RGB로 합쳐 Image 색으로 표시.
///       채널값 = floor(255 × 해당 색 스택 ÷ 세 스택 중 최댓값).  (모두 0이면 0)
///  - 값 출력: 각 스택의 원래 정수 값을 텍스트로 표시.
/// ColorStackChanged 이벤트를 구독해 값이 바뀔 때마다 갱신한다.
/// </summary>
public class ColorStackHUD : MonoBehaviour
{
    [Header("색상 출력 (변환 정수 → RGB)")]
    [SerializeField] Image colorSwatch;

    [Header("스택 값 출력 (원래 값)")]
    [SerializeField] TMP_Text redText;
    [SerializeField] TMP_Text greenText;
    [SerializeField] TMP_Text blueText;

    readonly int[] values = new int[3];

    void OnEnable()
    {
        EventBus.Subscribe<ColorStackChanged>(OnChanged);

        // 현재 스택 값으로 초기화(이벤트를 놓쳤을 수 있으므로 직접 읽음)
        var stacks = FindAnyObjectByType<ColorStacks>();
        if (stacks != null)
            for (int i = 0; i < 3; i++) values[i] = stacks.Get((LightColor)i);

        Refresh();
    }

    void OnDisable() => EventBus.Unsubscribe<ColorStackChanged>(OnChanged);

    void OnChanged(ColorStackChanged e)
    {
        values[(int)e.Color] = e.Value;
        Refresh();
    }

    void Refresh()
    {
        // 색상 출력 (공용 변환식 사용)
        if (colorSwatch)
            colorSwatch.color = ColorStacks.ToRGB(values[0], values[1], values[2]);

        // 스택 값(원래 정수) 출력
        if (redText) redText.text = values[(int)LightColor.Red].ToString();
        if (greenText) greenText.text = values[(int)LightColor.Green].ToString();
        if (blueText) blueText.text = values[(int)LightColor.Blue].ToString();
    }
}
