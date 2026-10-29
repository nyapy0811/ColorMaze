using System.Collections.Generic;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 맵 씬마다 하나 있는 맵 관리자. 맵 범위의 것은 <see cref="Current"/> 하나로 찾는다(씬과 함께 생기고 사라짐).
    /// 기물 등록부(맵 안 id → 기물)를 들고, 시작할 때 상태를 Playing으로 맞추고 플레이어를 시작 지점으로 옮긴다.
    /// 기물이 OnEnable에서 등록하므로 기물보다 먼저 깨어나도록 실행 순서를 앞에 둔다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MapManager : MonoBehaviour
    {
        public static MapManager Current { get; private set; }

        [Tooltip("맵 id. 맵 목록 애셋을 만들기 전까지는 씬마다 직접 적는다.")]
        [SerializeField] string mapId = "test_core_loop";
        [SerializeField] GameRulesConfig rules;
        [SerializeField] FirstPersonController player;

        readonly Dictionary<int, FixtureBase> fixtures = new();

        public string MapId => mapId;
        public GameRulesConfig Rules => rules;
        public FirstPersonController Player => player;

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            // 열린 맵에서 바로 시작하면 메뉴를 거치지 않으므로 여기서 Playing으로 맞춘다.
            if (GameManager.Instance.State != GameState.Playing) GameManager.Instance.StartGame();
            MovePlayerToStart();
        }

        public void Register(FixtureBase fixture)
        {
            if (fixtures.TryGetValue(fixture.FixtureId, out var other) && other != fixture)
            {
                Debug.LogError($"[MapManager] 맵 안 id {fixture.FixtureId}가 겹친다: '{other.name}', '{fixture.name}'. ColorMaze/맵 안 id 고치기로 고친다.", fixture);
                return;
            }
            fixtures[fixture.FixtureId] = fixture;
        }

        public void Unregister(FixtureBase fixture)
        {
            if (fixtures.TryGetValue(fixture.FixtureId, out var registered) && registered == fixture)
                fixtures.Remove(fixture.FixtureId);
        }

        void MovePlayerToStart()
        {
            foreach (var fixture in fixtures.Values)
            {
                if (fixture is StartPoint start)
                {
                    player.Teleport(start.transform.position, start.SpawnRotation);
                    return;
                }
            }
            Debug.LogError("[MapManager] 시작 지점이 없다.", this);
        }
    }
}
