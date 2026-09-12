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

    /// <summary>이번 프레임에 점프 키(Space)를 눌렀는지.</summary>
    public bool ReadJump()
    {
        var kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
    }

    /// <summary>이번 프레임에 일시정지 키(ESC)를 눌렀는지.</summary>
    public bool ReadPause()
    {
        var kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
    }

    /// <summary>이번 프레임에 설치 입력(좌클릭, Ctrl 안 누른 상태)을 눌렀는지 — 인게임 맵 에디터용.
    /// (우클릭은 자유 시점 카메라 회전 전용으로 바뀌어서 설치/제거 모두 좌클릭 계열로 이동함, 2026-09-12)</summary>
    public bool ReadInteract()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame && !ReadRemoveModifierHeld();
    }

    /// <summary>이번 프레임에 제거 입력(Ctrl+좌클릭)을 눌렀는지 — 인게임 맵 에디터용.</summary>
    public bool ReadRemove()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame && ReadRemoveModifierHeld();
    }

    /// <summary>우클릭을 누르고 있는 동안 true — 인게임 맵 에디터의 시점 회전(유니티 씬 뷰 방식).</summary>
    public bool ReadRotateHeld()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.rightButton.isPressed;
    }

    /// <summary>휠 클릭(가운데 버튼)을 누르고 있는 동안 true — 인게임 맵 에디터의 평행 이동(Pan).</summary>
    public bool ReadPanHeld()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.middleButton.isPressed;
    }

    /// <summary>이번 프레임의 스크롤 휠 세로 값 — 인게임 맵 에디터의 전/후 이동(Dolly)용.</summary>
    public float ReadDolly()
    {
        var mouse = Mouse.current;
        return mouse != null ? mouse.scroll.ReadValue().y : 0f;
    }

    /// <summary>Shift를 누르고 있는지 — 인게임 맵 에디터의 직사각형 범위 설치/제거 시작 조건.</summary>
    public bool ReadRangeModifierHeld()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
    }

    /// <summary>Ctrl을 누르고 있는지 — 좌클릭과 함께면 제거, Shift+드래그와 함께면 범위 제거.</summary>
    public bool ReadRemoveModifierHeld()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
    }

    /// <summary>이번 프레임에 확인 입력(스페이스바)을 눌렀는지 — 인게임 맵 에디터의 파라미터 패널
    /// Confirm 버튼과 동일한 동작을 키보드로도 쓸 수 있게.</summary>
    public bool ReadConfirm()
    {
        var kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
    }

    /// <summary>이번 프레임에 숫자 0 키(메인 자판/숫자패드 공용)를 눌렀는지.</summary>
    public bool ReadDigit0()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame);
    }

    /// <summary>이번 프레임에 숫자 1~8 키(메인 자판/숫자패드 공용) 중 눌린 게 있으면 그 번호(1~8)를
    /// slot에 담아 true를 반환한다 — 인게임 맵 에디터의 핫바 슬롯 선택용.</summary>
    public bool ReadHotBarSlot(out int slot)
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) { slot = 1; return true; }
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) { slot = 2; return true; }
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) { slot = 3; return true; }
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) { slot = 4; return true; }
            if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) { slot = 5; return true; }
            if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) { slot = 6; return true; }
            if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) { slot = 7; return true; }
            if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) { slot = 8; return true; }
        }
        slot = 0;
        return false;
    }
}
