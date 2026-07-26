using Framework.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조준점(크로스헤어) 색을 상호작용 가능 여부에 맞춰 바꾼다.
/// InteractionController가 발행하는 InteractableTargetChanged를 구독한다.
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [SerializeField] Image crosshairImage;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color highlightColor = Color.yellow;

    void OnEnable() => EventBus.Subscribe<InteractableTargetChanged>(OnTargetChanged);
    void OnDisable() => EventBus.Unsubscribe<InteractableTargetChanged>(OnTargetChanged);

    void OnTargetChanged(InteractableTargetChanged e)
    {
        if (crosshairImage) crosshairImage.color = e.HasTarget ? highlightColor : normalColor;
    }
}
