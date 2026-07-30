#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 필터(FilterBlockBase)들을 메시가 병합되는 규칙(같은 색 + 6방향 인접)과 동일한 기준으로 클러스터링해서
/// 종류별 폴더(ColorFilterN / RGBFilterN)로 재배치하는 공용 로직.
/// MazeGeneratorEditor(Scene 뷰 배치 도구)와 MapObjectOrganizer(하이어라키 정리 메뉴)가 함께 쓴다.
/// </summary>
public static class FilterClusterOrganizer
{
    public const string ColorFilterClusterPrefix = "ColorFilter";
    public const string RgbFilterClusterPrefix = "RGBFilter";

    public static bool IsClusterFolder(Transform t) =>
        t.name.StartsWith(ColorFilterClusterPrefix) || t.name.StartsWith(RgbFilterClusterPrefix);

    // parent 바로 아래의 모든 필터를 다시 스캔해서 클러스터 폴더(ColorFilterN / RGBFilterN)에 재배치한다.
    // 필터가 아닌 오브젝트는 건드리지 않는다.
    public static void Reorganize(Transform parent)
    {
        // 기존 클러스터 폴더를 지우기 전에 안의 필터들을 parent로 잠시 꺼내놓는다.
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (!IsClusterFolder(child)) continue;

            for (int j = child.childCount - 1; j >= 0; j--)
                child.GetChild(j).SetParent(parent, true);

            Undo.DestroyObjectImmediate(child.gameObject);
        }

        var filters = Object.FindObjectsByType<FilterBlockBase>(FindObjectsSortMode.None);
        var byCell = new Dictionary<Vector3Int, FilterBlockBase>();
        foreach (var f in filters) byCell[f.GridCell] = f;

        Vector3Int[] dirs =
        {
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 1, 0), new(0, -1, 0),
            new(0, 0, 1), new(0, 0, -1),
        };

        var visited = new HashSet<Vector3Int>();
        var clusters = new List<List<FilterBlockBase>>();

        foreach (var f in filters)
        {
            var start = f.GridCell;
            if (!visited.Add(start)) continue;

            var cluster = new List<FilterBlockBase>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var cur = byCell[cell];
                cluster.Add(cur);

                foreach (var d in dirs)
                {
                    var next = cell + d;
                    if (byCell.TryGetValue(next, out var nb) && SameColor(nb.RGB, cur.RGB) && visited.Add(next))
                        queue.Enqueue(next);
                }
            }

            clusters.Add(cluster);
        }

        // 매번 폴더 번호가 들쭉날쭉 바뀌지 않도록, 클러스터 안에서 가장 작은 칸 좌표 기준으로 정렬한다.
        clusters.Sort((a, b) => CompareCell(MinCell(a), MinCell(b)));

        // 필터 종류(컬러 필터/RGB 필터)별로 번호를 따로 매긴다.
        var nextIndex = new Dictionary<string, int>();
        foreach (var cluster in clusters)
        {
            string prefix = cluster[0] is ColorFilterBlock ? ColorFilterClusterPrefix : RgbFilterClusterPrefix;
            int index = nextIndex.TryGetValue(prefix, out var n) ? n : 1;
            nextIndex[prefix] = index + 1;

            var folder = new GameObject($"{prefix}{index}");
            Undo.RegisterCreatedObjectUndo(folder, "Create Filter Cluster Folder");
            folder.transform.SetParent(parent, false);

            foreach (var f in cluster)
                f.transform.SetParent(folder.transform, true);
        }
    }

    static bool SameColor(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;

    static Vector3Int MinCell(List<FilterBlockBase> cluster)
    {
        var min = cluster[0].GridCell;
        foreach (var f in cluster)
        {
            var c = f.GridCell;
            if (CompareCell(c, min) < 0) min = c;
        }
        return min;
    }

    static int CompareCell(Vector3Int a, Vector3Int b)
    {
        if (a.x != b.x) return a.x.CompareTo(b.x);
        if (a.y != b.y) return a.y.CompareTo(b.y);
        return a.z.CompareTo(b.z);
    }
}
#endif
