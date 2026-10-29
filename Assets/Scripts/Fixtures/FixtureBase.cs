using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// v2 기물의 기반. 맵 안 id(맵 안에서 1부터 늘어나는 정수, 에디터가 발급)를 들고, 켜질 때 맵 매니저에 등록한다.
    /// 강조와 상태 저장 기본 구현은 그것을 쓰는 단계에서 더한다.
    /// </summary>
    public abstract class FixtureBase : MonoBehaviour
    {
        [Tooltip("맵 안 id. 메뉴 ColorMaze/맵 안 id 고치기로 발급한다.")]
        [SerializeField] int fixtureId;

        public int FixtureId => fixtureId;

        protected virtual void OnEnable()
        {
            if (MapManager.Current != null) MapManager.Current.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (MapManager.Current != null) MapManager.Current.Unregister(this);
        }
    }
}
