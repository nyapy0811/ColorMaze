using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        var objects = Object.FindObjectsByType<MapObjectBase>(FindObjectsSortMode.None);

        if (objects.Length == 0)
        {
            Debug.Log("정리할 특수 블록이 없습니다.");
            return;
        }

        var rootGo = GameObject.Find(RootName);
        if (rootGo == null) rootGo = new GameObject(RootName);
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

        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Debug.Log($"특수 블록 {moved}개를 정리했습니다.");
    }
}
