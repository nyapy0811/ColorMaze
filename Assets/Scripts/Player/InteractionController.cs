using Framework.Core;
using UnityEngine;

/// <summary>조준 중인 상호작용 대상이 있는지(조준점 강조용)를 알린다. HasTarget이 바뀔 때만 발행한다.</summary>
public struct InteractableTargetChanged : IEvent
{
    public bool HasTarget;
}

/// <summary>
/// 조준+좌클릭으로 기물과 상호작용한다(걸어서 닿는 것과 별개의 추가 수단).
/// 매 프레임 카메라 정면으로 짧은 레이캐스트를 쏴서, 가장 가까운 대상이 IInteractable이면
/// 강조 표시(MapObjectBase.SetHighlighted)를 갱신하고, 좌클릭 시 TryInteract()를 호출한다.
/// 레이캐스트는 "가장 가까운 것 하나만 맞음" 특성상 벽이나 막힌 필터가 앞에 있으면 자연히 막힌다.
/// </summary>
public class InteractionController : MonoBehaviour
{
    [Tooltip("상호작용 가능 거리(칸 단위, 맵 그리드 1칸 = 1유닛)")]
    [SerializeField] float interactRange = 1.3f;

    Camera cam;
    MapObjectBase currentTarget;
    bool hasTarget;

    void Awake() => cam = Camera.main;

    void Update()
    {
        // 일시정지 중에는(Time.timeScale 0) 조준/상호작용 입력을 막는다(FirstPersonController와 동일한 규칙).
        if (Time.timeScale == 0f) return;
        if (cam == null) return;

        UpdateTarget();

        if (currentTarget != null && InputManager.Instance.ReadInteract())
            (currentTarget as IInteractable)?.TryInteract();
    }

    void UpdateTarget()
    {
        MapObjectBase hit = null;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hitInfo, interactRange,
                ~0, QueryTriggerInteraction.Collide))
        {
            var obj = hitInfo.collider.GetComponentInParent<MapObjectBase>();
            if (obj is IInteractable) hit = obj;
        }

        if (hit == currentTarget) return;

        currentTarget?.SetHighlighted(false);
        currentTarget = hit;
        currentTarget?.SetHighlighted(true);

        bool now = currentTarget != null;
        if (now != hasTarget)
        {
            hasTarget = now;
            EventBus.Publish(new InteractableTargetChanged { HasTarget = hasTarget });
        }
    }
}
