using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorMaze
{
    /// <summary>
    /// 맵 검사(첫 버전). 메뉴 ColorMaze/맵 검사로 지금 맵을 검사해 콘솔에 알린다(항목을 누르면 그 오브젝트로 감).
    /// 맵 씬을 저장할 때는 가벼운 검사(맵 안 id)만 자동으로 한다. 검사 항목은 기물을 만드는 단계마다 더한다.
    /// </summary>
    [InitializeOnLoad]
    public static class MapValidator
    {
        static MapValidator()
        {
            EditorSceneManager.sceneSaving += (scene, _) =>
            {
                if (IsMapScene(scene)) CheckFixtureIds(scene);
            };
        }

        [MenuItem("ColorMaze/맵 검사")]
        static void ValidateActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            int problems = Validate(scene);
            if (problems == 0) Debug.Log($"[맵 검사] {scene.name}: 문제 없음.");
            else Debug.LogWarning($"[맵 검사] {scene.name}: 문제 {problems}개.");
        }

        /// <summary>씬을 검사하고 문제 수를 돌려준다. 문제마다 콘솔에 남긴다.</summary>
        public static int Validate(Scene scene)
        {
            int problems = 0;

            var managers = Roots(scene).SelectMany(root => root.GetComponentsInChildren<MapManager>(true)).ToList();
            if (managers.Count != 1)
            {
                Debug.LogError($"[맵 검사] 맵 매니저가 {managers.Count}개다(1개여야 함).");
                problems++;
            }

            problems += CheckFixtureIds(scene);

            var starts = FixtureIdAssigner.FixturesIn(scene).OfType<StartPoint>().ToList();
            if (starts.Count != 1)
            {
                Debug.LogError($"[맵 검사] 시작 지점이 {starts.Count}개다(1개여야 함).", starts.FirstOrDefault());
                problems++;
            }

            var goals = FixtureIdAssigner.FixturesIn(scene).OfType<Goal>().ToList();
            if (goals.Count != 1)
            {
                Debug.LogError($"[맵 검사] 목적지가 {goals.Count}개다(1개여야 함).", goals.FirstOrDefault());
                problems++;
            }

            foreach (var source in FixtureIdAssigner.FixturesIn(scene).OfType<ColorSource>().Where(s => !OrbRules.IsValidSourceValue(s.Value)))
            {
                Debug.LogError($"[맵 검사] 추출 오브젝트 '{source.name}'의 값 {source.Value}은 쓸 수 없다((0,0,0)이나 세 채널이 같은 값).", source);
                problems++;
            }

            foreach (var submittable in FixtureIdAssigner.FixturesIn(scene).OfType<SubmittableObjectBase>().Where(s => !OrbRules.IsValidGoal(s.RequiredOrb)))
            {
                Debug.LogError($"[맵 검사] '{submittable.name}'의 요구 값 {submittable.RequiredOrb}은 쓸 수 없다(모든 채널 1 이상, 세 채널이 모두 같지 않아야 함).", submittable);
                problems++;
            }

            problems += CheckActuatorLinks(scene);
            problems += CheckNodeGraph(scene);

            return problems;
        }

        /// <summary>작동기 연결: 빈 칸, 두 문 계열에 든 작동기는 오류, 어느 목록에도 없는 작동기와 빈 목록은 경고.</summary>
        static int CheckActuatorLinks(Scene scene)
        {
            int problems = 0;
            var fixtures = FixtureIdAssigner.FixturesIn(scene).ToList();
            var gates = fixtures.OfType<GateBase>().ToList();
            foreach (var gate in gates)
            {
                if (gate.Actuators.Count == 0)
                {
                    Debug.LogWarning($"[맵 검사] 문 계열 '{gate.name}'에 연결된 작동기가 없다(늘 닫혀 있음).", gate);
                    problems++;
                }
                if (gate.Actuators.Any(actuator => actuator == null))
                {
                    Debug.LogError($"[맵 검사] 문 계열 '{gate.name}'의 작동기 목록에 빈 칸이 있다.", gate);
                    problems++;
                }
            }

            var links = gates.SelectMany(gate => gate.Actuators.Where(actuator => actuator != null).Distinct().Select(actuator => (actuator, gate))).ToList();
            foreach (var group in links.GroupBy(link => link.actuator).Where(g => g.Count() > 1))
            {
                Debug.LogError($"[맵 검사] 작동기 '{group.Key.name}'가 문 계열 여러 개에 연결돼 있다: {string.Join(", ", group.Select(link => $"'{link.gate.name}'"))}. 작동기 하나는 기물 하나에만 잇는다.", group.Key);
                problems++;
            }
            foreach (var actuator in fixtures.OfType<ActuatorBase>().Where(a => links.All(link => link.actuator != a)))
            {
                Debug.LogWarning($"[맵 검사] 작동기 '{actuator.name}'가 어느 문 계열에도 연결돼 있지 않다.", actuator);
                problems++;
            }
            return problems;
        }

        /// <summary>노드 그래프 1개, 같은 자리 노드, 없는 노드를 가리키는 벽, 높이가 다른 벽, 끼울 수 없는 문 위치.</summary>
        static int CheckNodeGraph(Scene scene)
        {
            var graphs = Roots(scene).SelectMany(root => root.GetComponentsInChildren<NodeGraph>(true)).ToList();
            if (graphs.Count != 1)
            {
                Debug.LogError($"[맵 검사] 노드 그래프가 {graphs.Count}개다(1개여야 함). ColorMaze/노드 편집 창에서 만든다.");
                return 1;
            }

            int problems = 0;
            var graph = graphs[0];
            var data = graph.Data;
            foreach (var group in data.nodes.GroupBy(node => node.position).Where(g => g.Count() > 1))
            {
                Debug.LogError($"[맵 검사] 같은 자리 {group.Key}에 노드가 {group.Count()}개다. 노드 편집 창의 '다시 만들기'가 합친다.", graph);
                problems++;
            }

            var config = NodeGraphEditing.FindConfig();
            foreach (var wall in data.walls)
            {
                var start = NodeGraphRules.FindNode(data, wall.startNodeId);
                var end = NodeGraphRules.FindNode(data, wall.endNodeId);
                if (start == null || end == null)
                {
                    Debug.LogError($"[맵 검사] 벽 #{wall.id}이 없는 노드를 가리킨다.", graph);
                    problems++;
                }
                else if (start.position.y != end.position.y)
                {
                    Debug.LogError($"[맵 검사] 벽 #{wall.id}의 두 노드 높이가 다르다(벽은 y가 같은 노드끼리만 잇는다).", graph);
                    problems++;
                }
                else if (wall.kind == WallKind.Normal && wall.hasDoor && config != null
                         && !NodeGraphRules.CanPlaceDoor(NodeGraphRules.SegmentLength(start.position, end.position), config.doorWidth, wall.door.offsetModules))
                {
                    Debug.LogError($"[맵 검사] 벽 #{wall.id}의 문을 끼울 수 없는 위치다(구간이 짧거나 문이 끝에 붙음). 노드 편집 창에서 문 위치를 고친다.", graph);
                    problems++;
                }
            }
            return problems;
        }

        /// <summary>맵 안 id의 빈 id와 겹침을 알리고 문제 수를 돌려준다.</summary>
        static int CheckFixtureIds(Scene scene)
        {
            int problems = 0;
            var fixtures = FixtureIdAssigner.FixturesIn(scene).ToList();
            foreach (var fixture in fixtures.Where(f => f.FixtureId <= 0))
            {
                Debug.LogError($"[맵 검사] '{fixture.name}'의 맵 안 id가 비어 있다. ColorMaze/맵 안 id 고치기로 고친다.", fixture);
                problems++;
            }
            foreach (var group in fixtures.Where(f => f.FixtureId > 0).GroupBy(f => f.FixtureId).Where(g => g.Count() > 1))
            {
                Debug.LogError($"[맵 검사] 맵 안 id {group.Key}가 겹친다: {string.Join(", ", group.Select(f => $"'{f.name}'"))}. ColorMaze/맵 안 id 고치기로 고친다.", group.First());
                problems++;
            }
            return problems;
        }

        static bool IsMapScene(Scene scene) => Roots(scene).Any(root => root.GetComponentInChildren<MapManager>(true) != null);

        static GameObject[] Roots(Scene scene) => scene.GetRootGameObjects();
    }
}
