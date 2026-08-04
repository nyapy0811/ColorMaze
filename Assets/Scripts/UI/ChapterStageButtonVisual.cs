using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 챕터/스테이지 버튼의 잠김/해금/클리어 상태별 스프라이트(평상시+눌림)를 저장해두고,
/// 상태가 바뀔 때 Image.sprite와 Button.spriteState.pressedSprite를 갈아끼운다.
/// Button.Transition은 SpriteSwap을 그대로 사용한다 — 눌림 감지는 Button이 처리하므로
/// 이 스크립트는 포인터 이벤트를 직접 다루지 않는다.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class ChapterStageButtonVisual : MonoBehaviour
{
    public enum State { Locked, Unlocked, Cleared }

    [Header("잠김")]
    [SerializeField] Sprite lockedSprite;
    [SerializeField] Sprite lockedPressedSprite;

    [Header("해금")]
    [SerializeField] Sprite unlockedSprite;
    [SerializeField] Sprite unlockedPressedSprite;

    [Header("클리어")]
    [SerializeField] Sprite clearedSprite;
    [SerializeField] Sprite clearedPressedSprite;

    Button button;
    Image image;

    public void SetState(State state)
    {
        if (!button) button = GetComponent<Button>();
        if (!image) image = GetComponent<Image>();

        var (normal, pressed) = state switch
        {
            State.Locked => (lockedSprite, lockedPressedSprite),
            State.Cleared => (clearedSprite, clearedPressedSprite),
            _ => (unlockedSprite, unlockedPressedSprite),
        };

        image.sprite = normal;
        var spriteState = button.spriteState;
        spriteState.pressedSprite = pressed;
        button.spriteState = spriteState;
    }
}
