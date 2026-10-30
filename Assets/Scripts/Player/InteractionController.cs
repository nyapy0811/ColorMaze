using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 조준 컨트롤러. 매 프레임 조준 카메라의 앞(화면 중앙)으로 상호작용 거리만큼 레이캐스트해 조준 대상을 고르고,
    /// 좌클릭하면 대상과 상호작용한다. 가장 가까운 것 하나만 맞으므로 벽 너머는 안 되고, 큰 기물은 콜라이더 전체가 대상이다.
    /// 입력이 잠겨 있거나 일시정지 중이면 아무것도 하지 않는다. 조준 값 표시('조준 대상 바뀜')는 코어 루프 8단계에서 더한다.
    /// 뷰모델에는 콜라이더가 없어 레이캐스트에 걸리지 않는다.
    /// </summary>
    [RequireComponent(typeof(PlayerInventory), typeof(FirstPersonController))]
    public class InteractionController : MonoBehaviour
    {
        [Tooltip("레이캐스트를 쏘는 카메라(눈높이)")]
        [SerializeField] Transform aimCamera;

        PlayerContext context;

        /// <summary>지금 조준한 기물. 없으면 null.</summary>
        public FixtureBase Target { get; private set; }

        void Awake() => context = new PlayerContext(GetComponent<PlayerInventory>(), GetComponent<FirstPersonController>());

        void Update()
        {
            if (InputManager.Instance.IsGameplayLocked || Time.timeScale == 0f)
            {
                Target = null;
                return;
            }
            Target = FindTarget();
            if (Target is IInteractable { CanInteract: true } interactable && InputManager.Instance.ReadInteract())
                interactable.Interact(context);
        }

        FixtureBase FindTarget()
        {
            float distance = MapManager.Current.Rules.interactDistance;
            if (!Physics.Raycast(aimCamera.position, aimCamera.forward, out var hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return null;
            return hit.collider.GetComponentInParent<FixtureBase>();
        }
    }
}
