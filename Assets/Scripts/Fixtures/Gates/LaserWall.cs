using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 레이저 벽(6.5). 모양은 같은 오브젝트의 <see cref="LaserSegment"/>(노드 시스템)가 맡고, 이 컴포넌트는 막음을 맡는다(11.4).
    /// 요구 색은 빼는 색이다: 지금 색의 모든 채널이 빼는 색 이상이면 막지 않고(레이저가 흐려짐), 레이저를 다 지나 반대쪽으로
    /// 나가면 지금 색에서 빼고 '레이저 통과'를 보낸다(들어갔다 되돌아오면 빼지 않음). 모자라면 막는다. 상호작용은 없다.
    /// 지나감은 막는 콜라이더와 같은 자리의 트리거로 잰다: 들어온 쪽과 나간 쪽이 다르면 지나간 것이다.
    /// 플레이어가 레이저 자리에 있는 동안은 막지 않던 레이저를 켜지 않는다(플레이어 위로 켜지지 않게).
    /// </summary>
    [RequireComponent(typeof(LaserSegment))]
    public class LaserWall : GateBase
    {
        [SerializeField] LaserWallView view;

        bool canPass;
        bool inside;
        float entrySide;

        // 레이저 면은 루트 바로 아래 자식이고(LaserSegment) 루트는 늘이지 않는다.
        protected override void Awake()
        {
            base.Awake();
            var blocker = Blockers[0];
            var trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = transform.InverseTransformPoint(blocker.transform.TransformPoint(blocker.center));
            trigger.size = Vector3.Scale(blocker.size, blocker.transform.localScale);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus.Subscribe<CurrentColorChanged>(OnCurrentColorChanged);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<CurrentColorChanged>(OnCurrentColorChanged);
        }

        void OnCurrentColorChanged(CurrentColorChanged e)
        {
            canPass = ColorRules.IsAtLeast(e.Value, RequiredColor);
            Refresh();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PlayerColor _)) return;
            inside = true;
            entrySide = Side(other.transform.position);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out PlayerColor player)) return;
            inside = false;
            if (Side(other.transform.position) != entrySide)
            {
                player.Current.Pass(RequiredColor);
                EventBus.Publish(new LaserPassed());
            }
            Refresh();
        }

        void Refresh()
        {
            bool blocking = !canPass;
            if (blocking == IsBlocking || (blocking && inside)) return;
            SetBlocking(blocking);
            view.SetPassable(!blocking);
        }

        float Side(Vector3 position) => Mathf.Sign(Vector3.Dot(position - transform.position, transform.forward));
    }
}
