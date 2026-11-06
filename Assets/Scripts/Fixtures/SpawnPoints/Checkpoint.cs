using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 체크포인트(출발점 계열). 플레이어가 처음 닿으면 활성화되고 맵 매니저에 스냅샷을 요청한다(이미 활성화되면 무시, 2차 Q450).
    /// 돌아오면 플레이어는 이 오브젝트의 자리와 방향에 선다. 닿음은 같은 오브젝트의 트리거 콜라이더로 잰다.
    /// 활성화 여부는 기물 상태가 아니라 스냅샷의 '활성화된 체크포인트 목록'에 든다. 겉모습은 임시(활성화되면 표시 색이 바뀜).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : SpawnPointBase
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("활성화 여부를 색으로 보일 렌더러(임시 겉모습)")]
        [SerializeField] Renderer marker;
        [SerializeField] Color inactiveColor = new(0.6f, 0.6f, 0.6f);
        [SerializeField] Color activeColor = new(0.3f, 0.9f, 1f);

        MaterialPropertyBlock block;

        public bool IsActivated { get; private set; }

        void Awake() => Paint();

        void OnTriggerEnter(Collider other)
        {
            if (IsActivated || !other.TryGetComponent(out PlayerColor _)) return;
            MapManager.Current.ReachCheckpoint(this);
        }

        /// <summary>맵 매니저가 부른다(처음 닿음, 체크포인트 복귀).</summary>
        public void SetActivated(bool activated)
        {
            IsActivated = activated;
            Paint();
        }

        void Paint()
        {
            if (marker == null) return;
            block ??= new MaterialPropertyBlock();
            marker.GetPropertyBlock(block);
            block.SetColor(BaseColorId, IsActivated ? activeColor : inactiveColor);
            marker.SetPropertyBlock(block);
        }
    }
}
