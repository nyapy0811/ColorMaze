using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [개발자 전용] 씬의 특수 블록(MapObjectBase 파생 — 필터/팔레트/체인저/버킷/캔버스 등)을
/// MapObjects 폴더 아래로 모아 하이어라키에서 일반 미로 블록과 구분되게 정리한다.
/// 필터가 아닌 기물은 폴더 없이 MapObjects 바로 밑에 두고, 필터만 메시를 공유하는(같은 색+인접) 것끼리
/// 종류별 클러스터 폴더(ColorFilterN / RGBFilterN)로 묶는다.
/// 메뉴에서 수동으로 실행한다. 콜라이더·위치는 그대로 유지되고(월드 좌표 보존) 부모만 바뀐다.
/// </summary>
public static class MapObjectOrganizer
{
    const string RootName = "MapObjects";

    [MenuItem("ColorMaze/특수 블록 하이어라키 정리")]
    static void Organize()
    {
        int moved = OrganizeScene(SceneManager.GetActiveScene());
        Debug.Log($"특수 블록 {moved}개를 정리했습니다.");
    }

    [MenuItem("ColorMaze/특수 블록 하이어라키 정리 (전체 스테이지 일괄)")]
    static void OrganizeAllStages()
    {
        if (!EditorUtility.DisplayDialog("전체 스테이지 하이어라키 정리",
                "Assets/Scenes 아래 모든 씬을 열어서 특수 블록 하이어라키를 정리합니다.\n" +
                "되돌리기 어려우니 먼저 Git 등으로 백업해두는 걸 권장합니다.\n계속할까요?",
                "계속", "취소"))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();

        int scenesTouched = 0;
        int totalMoved = 0;

        foreach (var path in paths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int moved = OrganizeScene(scene);
            if (moved > 0)
            {
                scenesTouched++;
                totalMoved += moved;
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[MapObjectOrganizer] {path} — {moved}개 정리.");
        }

        EditorUtility.DisplayDialog("전체 스테이지 하이어라키 정리",
            $"완료됐습니다.\n전체 {paths.Count}개 씬 중 {scenesTouched}개 씬에서 특수 블록 {totalMoved}개를 정리했습니다.",
            "확인");
    }

    // 현재 로드된 씬 하나를 정리하고, 이동시킨 특수 블록 개수를 반환한다.
    static int OrganizeScene(Scene scene)
    {
        var objects = Object.FindObjectsByType<MapObjectBase>(FindObjectsSortMode.None)
            .Where(o => o.gameObject.scene == scene)
            .ToArray();

        if (objects.Length == 0)
            return 0;

        var rootGo = FindInScene(scene, RootName);
        if (rootGo == null)
        {
            rootGo = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(rootGo, scene);
        }
        var root = rootGo.transform;

        int moved = 0;
        foreach (var obj in objects)
        {
            // 필터도 일단 MapObjects 바로 밑으로 옮겨두고, 아래 Reorganize()가 클러스터 폴더로 다시 정리한다.
            // 필터가 아닌 기물은 폴더 없이 여기 그대로 남는다.
            if (obj.transform.parent != root)
            {
                Undo.SetTransformParent(obj.transform, root, "특수 블록 정리");
                moved++;
            }
        }

        FilterClusterOrganizer.Reorganize(root);

        // 예전 방식(타입별 폴더)으로 만들어졌던 빈 폴더가 남아있으면 정리한다.
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (child.childCount == 0 && child.GetComponents<Component>().Length == 1)
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        return moved;
    }

    static GameObject FindInScene(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }
}
