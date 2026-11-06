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

            foreach (var source in FixtureIdAssigner.FixturesIn(scene).OfType<ColorSource>().Where(s => !ColorRules.IsValidSourceValue(s.Value)))
            {
                Debug.LogError($"[맵 검사] 색 원천 '{source.name}'의 값 {source.Value}은 쓸 수 없다((0,0,0)이나 세 채널이 같은 값).", source);
                problems++;
            }

            var rules = managers.Count == 1 ? managers[0].Rules : null;
            if (rules == null) Debug.LogWarning("[맵 검사] 게임 규칙을 찾지 못해 요구 색을 검사하지 않았다(맵 매니저의 게임 규칙 칸).");
            else problems += CheckRequiredColors(scene, rules.channelLimit);

            problems += CheckNodeGraph(scene);

            if (managers.Count == 1) problems += CheckMapList(scene, managers[0].MapList);

            return problems;
        }

        /// <summary>맵 목록: 맵 매니저에 연결됐는지, 이 씬이 목록에 있는지, 맵 id가 비었거나 겹치는지.</summary>
        static int CheckMapList(Scene scene, MapList mapList)
        {
            if (mapList == null)
            {
                Debug.LogError("[맵 검사] 맵 매니저에 맵 목록(Assets/Data/MapList)이 연결되지 않았다.");
                return 1;
            }
            int problems = 0;
            if (mapList.FindByScene(scene.name) == null)
            {
                Debug.LogError($"[맵 검사] 맵 목록에 이 씬('{scene.name}')이 없다.", mapList);
                problems++;
            }
            foreach (var entry in mapList.maps.Where(entry => string.IsNullOrWhiteSpace(entry.mapId)))
            {
                Debug.LogError($"[맵 검사] 맵 목록의 '{entry.sceneName}' 항목에 맵 id가 없다.", mapList);
                problems++;
            }
            foreach (var group in mapList.maps.Where(entry => !string.IsNullOrWhiteSpace(entry.mapId)).GroupBy(entry => entry.mapId).Where(g => g.Count() > 1))
            {
                Debug.LogError($"[맵 검사] 맵 목록에 맵 id '{group.Key}'가 {group.Count()}개다(맵마다 달라야 함).", mapList);
                problems++;
            }
            return problems;
        }

        /// <summary>요구 색: 문·목적지는 요구 색 규칙, 레이저 벽의 빼는 색은 색 원천 값 규칙에 더해 한도 안이어야 지날 수 있다.</summary>
        static int CheckRequiredColors(Scene scene, ColorValue limit)
        {
            int problems = 0;
            foreach (var fixture in FixtureIdAssigner.FixturesIn(scene).OfType<RequiredColorBase>())
            {
                var color = fixture.RequiredColor;
                if (fixture is LaserWall)
                {
                    if (ColorRules.IsValidSourceValue(color) && ColorRules.IsWithin(color, limit)) continue;
                    Debug.LogError($"[맵 검사] 레이저 벽 '{fixture.name}'의 빼는 색 {color}은 쓸 수 없다((0,0,0)이나 세 채널이 같은 값, 또는 한도 {limit}를 넘음).", fixture);
                }
                else
                {
                    if (ColorRules.IsValidRequired(color, limit)) continue;
                    Debug.LogError($"[맵 검사] '{fixture.name}'의 요구 색 {color}은 쓸 수 없다(모든 채널 1 이상, 세 채널이 모두 같지 않고, 한도 {limit} 안이어야 함).", fixture);
                }
                problems++;
            }
            return problems;
        }

        /// <summary>노드 그래프 1개, 같은 자리 노드, 높이(y)가 0이 아닌 노드, 없는 노드를 가리키는 벽, 끼울 수 없는 문 위치.</summary>
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
            foreach (var node in data.nodes.Where(node => node.position.y != 0))
            {
                Debug.LogError($"[맵 검사] 노드 #{node.id}의 높이가 {node.position.y}다(맵은 한 층이라 y는 0이어야 함). 노드 편집 도구로 옮기면 y가 0이 된다.", graph);
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
