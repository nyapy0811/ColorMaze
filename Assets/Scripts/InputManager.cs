using UnityEngine;
using UnityEngine.InputSystem;
using Framework.Core;

/// <summary>
/// 새 Input System 입력을 한 곳에서 읽어 이동/시점 입력으로 변환한다.
/// 지금은 키보드+마우스만 처리하며, 나중에 터치/패드를 여기서만 추가하면 된다.
///   W/↑ = 전진,  S/↓ = 후진,  A/← = 좌,  D/→ = 우,  마우스 = 시점
/// </summary>
public class InputManager : MonoSingleton<InputManager>
{
    /// <summary>이동 입력을 반환한다. x = 좌우, y = 전후 (-1~1).</summary>
    public Vector2 ReadMove()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        return new Vector2(x, y);
    }

    /// <summary>마우스 시점 입력(이 프레임의 이동량)을 반환한다. x = 좌우, y = 상하.</summary>
    public Vector2 ReadLook()
    {
        var mouse = Mouse.current;
        return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
    }
}
