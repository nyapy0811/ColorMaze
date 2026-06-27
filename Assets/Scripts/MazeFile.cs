using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 맵 편집 데이터를 JSON 파일로 저장/로드하는 공용 헬퍼.
/// 플레이 중 편집을 파일에 저장하고, 플레이 종료 시 에디터가 읽어 씬에 반영한다.
/// (애셋/파일은 플레이 모드 종료 시 되돌려지지 않으므로 씬 반영의 다리 역할.)
/// </summary>
public static class MazeFile
{
    [Serializable] public class FloorDTO { public string[] rows; }
    [Serializable] public class Model { public FloorDTO[] floors; }

    public static string Path =>
        System.IO.Path.Combine(Application.persistentDataPath, "maze_autosave.json");

    public static void Save(Model m) =>
        File.WriteAllText(Path, JsonUtility.ToJson(m, true));

    public static bool TryLoad(out Model m)
    {
        m = null;
        if (!File.Exists(Path)) return false;
        m = JsonUtility.FromJson<Model>(File.ReadAllText(Path));
        return m != null && m.floors != null && m.floors.Length > 0;
    }

    public static void Delete()
    {
        if (File.Exists(Path)) File.Delete(Path);
    }
}
