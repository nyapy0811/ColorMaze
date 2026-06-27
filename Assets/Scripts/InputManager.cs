using UnityEngine;
using UnityEngine.InputSystem;
using Framework.Core;

/// <summary>
/// 그리드 이동 입력 명령.
/// </summary>
public enum MoveCommand { None, Forward, Back, TurnLeft, TurnRight }

/// <summary>
/// 새 Input System 입력을 한 곳에서 읽어 이동 명령으로 변환한다.
/// 지금은 키보드만 처리하며, 나중에 터치/스와이프를 여기서만 추가하면 된다.
///   W/↑ = 전진,  S/↓ = 후진,  A/← = 좌회전,  D/→ = 우회전
/// </summary>
public class InputManager : MonoSingleton<InputManager>
{
    /// <summary>이번 프레임에 눌린 이동 명령을 반환한다(없으면 None).</summary>
    public MoveCommand Read()
    {
        var kb = Keyboard.current;
        if (kb == null) return MoveCommand.None;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) return MoveCommand.Forward;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) return MoveCommand.Back;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) return MoveCommand.TurnRight;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) return MoveCommand.TurnLeft;
        return MoveCommand.None;
    }
}
