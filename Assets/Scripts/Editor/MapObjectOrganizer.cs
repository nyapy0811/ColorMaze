using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// [개발자 전용] 씬의 특수 블록(MapObjectBase 파생 — 필터/팔레트/체인저/지우개/캔버스 등)을
/// 타입별 폴더(GameObject)로 묶어 하이어라키에서 일반 미로 블록과 구분되게 정리한다.
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

        var root = GameObject.Find(RootName);
        if (root == null) root = new GameObject(RootName);

        int moved = 0;
        foreach (var obj in objects)
        {
            string typeName = obj.GetType().Name;
            var folder = root.transform.Find(typeName);
            if (folder == null)
            {
                var folderGo = new GameObject(typeName);
                folderGo.transform.SetParent(root.transform, false);
                folder = folderGo.transform;
            }

            if (obj.transform.parent != folder)
            {
                Undo.SetTransformParent(obj.transform, folder, "특수 블록 정리");
                moved++;
            }
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"특수 블록 {moved}개를 타입별 폴더로 정리했습니다.");
    }
}
