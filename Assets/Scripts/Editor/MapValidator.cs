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
