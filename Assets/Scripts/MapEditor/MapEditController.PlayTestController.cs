using System.Collections.Generic;
using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class MapEditController
{
    // 플레이 테스트(그 자리에서 바로 플레이해보기 + 정답 순서 자동 기록) 전담.
    class PlayTestController
    {
        readonly MapEditController owner;

        public PlayTestController(MapEditController owner) => this.owner = owner;

        bool isPlayTesting;
        public bool IsPlayTesting => isPlayTesting;

        MazeGenerator playTestMaze; // 첫 테스트 때 한 번만 추가, 이후 재사용
        static readonly Vector3 PlayerSpawnPosition = new(0f, 0.5f, 0f);

        // 정답 순서 자동 기록용 — 테스트 중 상호작용한 기물 id를 순서대로 쌓다가, 캔버스가 클리어되면
        // 그때까지 쌓인 걸 그 캔버스 순서로 확정하고 비운다(다음 캔버스는 그 시점부터 새로 기록).
        readonly List<int> playTestUsedSequence = new();
        Dictionary<MapObjectBase, int> playTestFixtureIdByInstance;

        /// <summary>PlayModePanel의 PlayButton OnClick — 지금 편집 중인 맵을 그 자리에서 플레이 테스트한다.
        /// 새로 Instantiate하지 않고 이미 배치돼 있는 오브젝트를 그대로 재생 가능한 상태로 전환한다.</summary>
        public void StartPlayTest()
        {
            if (isPlayTesting) return;

            Time.timeScale = 1f; // 직전 클리어 화면 등으로 멈춰 있었을 수 있으니 항상 명시적으로 복구.

            owner.fixtureEditController.ResetEditingSelection();
            owner.fixtureEditController.HideParamPanels();
            owner.fixtureEditController.ClearMarks();
            owner.placementController.HideHoverPreview();
            owner.placementController.ClearDragPreview();
            owner.placementController.dragging = false;

            if (owner.selectionPanel != null) owner.selectionPanel.SetActive(false);
            if (owner.playTestOverlay != null) owner.playTestOverlay.SetActive(true);

            var fpc = FirstPersonController.Instance;
            if (fpc != null)
            {
                fpc.Teleport(PlayerSpawnPosition, Quaternion.identity);
                var flyCam = fpc.GetComponent<EditorFlyCamera>();
                if (flyCam != null) flyCam.enabled = false;
                fpc.enabled = true;
                var interact = fpc.GetComponent<InteractionController>();
                if (interact != null) interact.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            ColorStacks.Instance?.ResetAll();

            if (playTestMaze == null) playTestMaze = owner.gameObject.AddComponent<MazeGenerator>();
            var byId = new Dictionary<int, MapObjectBase>();
            playTestFixtureIdByInstance = new Dictionary<MapObjectBase, int>();
            playTestUsedSequence.Clear();
            foreach (var placed in owner.cells.Values)
                if (placed.Fixture != null)
                {
                    var obj = placed.GameObject.GetComponent<MapObjectBase>();
                    if (obj == null) continue;
                    byId[placed.Fixture.id] = obj;
                    playTestFixtureIdByInstance[obj] = placed.Fixture.id;
                    // 씬을 다시 로드하지 않고 같은 오브젝트를 재사용하므로, 이전 테스트에서 완료된 캔버스가
                    // 이번 테스트에도 이미 완료된 채로 남아있지 않도록 매번 초기화한다.
                    if (obj is ClearObjectBase clearObj) clearObj.ResetCompletion();
                }

            playTestMaze.correctOrders.Clear();
            foreach (var order in owner.data.canvasOrders)
            {
                var list = new List<MapObjectBase>();
                foreach (var id in order.orderFixtureIds)
                    if (byId.TryGetValue(id, out var obj)) list.Add(obj);
                playTestMaze.correctOrders.Add(list);
            }

            FilterBlockBase.RebuildAll();
            EventBus.Publish(new SceneLoadCompleted { SceneName = SceneManager.GetActiveScene().name });

            isPlayTesting = true;
            // 일반 Playing이 아니라 별도 상태를 쓴다 — GameManager.StageClear()의 "Playing일 때만" 가드에
            // 걸려 실제 클리어(GameState.Cleared) 전환 자체가 막히므로, ClearScreenController 등 클리어에
            // 반응하는 쪽에서 별도로 플레이 테스트 여부를 확인할 필요가 없다.
            GameManager.Instance.ChangeState(GameState.MapEditorPlayTest);
        }

        /// <summary>PlayTestOverlay의 "에디터로 돌아가기" OnClick, 그리고 테스트 중 클리어 감지 시 자동 호출.</summary>
        public void StopPlayTest()
        {
            if (!isPlayTesting) return;

            Time.timeScale = 1f;
            GameManager.Instance.ChangeState(GameState.MapEditor);

            var fpc = FirstPersonController.Instance;
            if (fpc != null)
            {
                fpc.enabled = false;
                var interact = fpc.GetComponent<InteractionController>();
                if (interact != null) interact.enabled = false;
                var flyCam = fpc.GetComponent<EditorFlyCamera>();
                if (flyCam != null) flyCam.enabled = true;
            }

            ColorStacks.Instance?.ResetAll();
            RestoreConsumedFixtures();

            if (owner.playTestOverlay != null) owner.playTestOverlay.SetActive(false);
            if (owner.selectionPanel != null) owner.selectionPanel.SetActive(true);

            playTestUsedSequence.Clear();
            playTestFixtureIdByInstance = null;
            isPlayTesting = false;
        }

        // 버킷/팔레트/컬러 체인저/스택 체인저 같은 소모성 기물은 실제로 사용되면
        // ConsumableObjectBase.Consume()이 오브젝트를 완전히 Destroy()해버린다(캔버스처럼 완료 상태만
        // 잠그는 게 아니라 진짜로 사라짐). data.fixtures엔 그대로 남아있으니, 에디터로 돌아올 때 파괴된
        // 인스턴스만 골라 CustomStageLoader.PlaceFixture로 다시 만들어 원상 복구한다 — 저장 안 한 편집
        // 내용이 플레이 테스트 한 번으로 사라지면 안 되기 때문("클리어 후 에디터로 복귀시 사라진 기물이
        // 안 돌아온다" 버그로 발견됨).
        void RestoreConsumedFixtures()
        {
            foreach (var placed in owner.cells.Values)
            {
                if (placed.Fixture == null || placed.GameObject != null) continue;
                var instance = CustomStageLoader.PlaceFixture(placed.Fixture, owner.prefabs, owner.mapObjectsRoot);
                if (instance != null) placed.GameObject = instance.gameObject;
            }
        }

        // 테스트 중 기물을 상호작용할 때마다 호출(필터 통과, 획득, 소모, 캔버스 완료 등 전부 포함).
        // 캔버스 자신의 완료는 "재료"가 아니라 목표이므로 순서 기록에서 제외한다.
        public void OnMapObjectUsedDuringPlayTest(MapObjectUsed e)
        {
            if (!isPlayTesting || e.Source is ClearObjectBase) return;
            if (playTestFixtureIdByInstance != null && playTestFixtureIdByInstance.TryGetValue(e.Source, out int id))
                playTestUsedSequence.Add(id);
        }

        // 캔버스 하나가 클리어되면 그때까지 쌓인 상호작용 순서를 그 캔버스의 정답 순서로 확정(덮어쓰기)하고
        // 다음 캔버스를 위해 비운다 — 이렇게 실제 플레이로 검증된 순서가 수동 입력 없이 자동으로 저장된다.
        public void OnCanvasCompletedDuringPlayTest(CanvasCompleted e)
        {
            if (!isPlayTesting) return;
            if (playTestFixtureIdByInstance == null || !playTestFixtureIdByInstance.TryGetValue(e.Source, out int canvasId)) return;

            var order = owner.data.canvasOrders.Find(o => o.canvasFixtureId == canvasId);
            if (order != null)
            {
                order.orderFixtureIds.Clear();
                order.orderFixtureIds.AddRange(playTestUsedSequence);
            }
            playTestUsedSequence.Clear();
        }

        // 테스트 중 실제로 정답 순서를 전부 맞춰 클리어하면(LevelManager가 StageCleared를 발행하고 곧이어
        // GameManager.StageClear()를 호출), 진짜 클리어 화면 대신 자동으로 에디터로 돌아간다.
        // 플레이 테스트는 Playing이 아니라 MapEditorPlayTest 상태라 StageClear()의 가드에 걸려 실제
        // GameState.Cleared 전환 자체가 안 일어나고, ClearScreenController도 State로 판단해 클리어
        // 화면을 안 띄우므로, 여기서는 상태 복구만 하면 된다. 클리어까지 확인됐다는 표시로
        // data.clearVerified도 true로 세팅.
        public void OnStageClearedDuringPlayTest(StageCleared e)
        {
            if (!isPlayTesting) return;
            owner.data.clearVerified = true;
            StopPlayTest();
        }
    }
}
