using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [예시] 입력으로 스택을 증감한다. 1/2/3 키로 Red/Green/Blue +1.
/// 실제 게임 규칙(어떤 입력에 어느 스택을 얼마)에 맞게 자유롭게 수정할 것.
/// </summary>
[RequireComponent(typeof(ColorStacks))]
public class ColorStackInput : MonoBehaviour
{
    ColorStacks stacks;

    void Awake() => stacks = GetComponent<ColorStacks>();

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) stacks.Add(LightColor.Red, 1);
        if (kb.digit2Key.wasPressedThisFrame) stacks.Add(LightColor.Green, 1);
        if (kb.digit3Key.wasPressedThisFrame) stacks.Add(LightColor.Blue, 1);
    }
}
