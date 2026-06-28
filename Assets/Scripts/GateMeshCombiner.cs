using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 같은 RGB를 가진 게이트 블록들을 색상별로 하나의 메시로 합친다.
/// 서로 맞닿은 내부 면은 생성하지 않아 투명 블록 사이의 '막'이 사라진다.
/// 개별 ColorGateBlock(콜라이더·통과 로직)은 그대로 남아 블록 단위 삭제가 가능하며,
/// 시각 표시만 합친 메시가 담당한다(개별 렌더러는 끔).
/// </summary>
public class GateMeshCombiner : MonoBehaviour
{
    [Header("게이트 블록 공용 설정")]
    [Tooltip("게이트 블록에 적용할 머티리얼(Transparent로 만들어 둔 것)")]
    public Material gateMaterial;

    [Tooltip("게이트 블록 색의 투명도(0 = 완전 투명, 1 = 불투명)")]
    [Range(0f, 1f)] public float gateAlpha = 0.5f;

    static readonly Vector3Int[] Dirs =
    {
        new(1, 0, 0), new(-1, 0, 0),
        new(0, 1, 0), new(0, -1, 0),
        new(0, 0, 1), new(0, 0, -1),
    };

    void Start() => Rebuild();

    /// <summary>게이트들을 색상별로 다시 병합한다(블록 추가/삭제 후 호출).</summary>
    public void Rebuild()
    {
        var root = PrepareRoot();

        var gates = FindObjectsByType<ColorGateBlock>(FindObjectsSortMode.None);
        var groups = new Dictionary<Color32, List<ColorGateBlock>>(new Color32RGBComparer());

        foreach (var g in gates)
        {
            g.SetRendererEnabled(false); // 개별 렌더 끄고 병합 메시로 표시
            if (!groups.TryGetValue(g.RGB, out var list))
                groups[g.RGB] = list = new List<ColorGateBlock>();
            list.Add(g);
        }

        foreach (var kv in groups)
            BuildGroup(root, kv.Key, kv.Value, gateMaterial, gateAlpha);
    }

    // 인스펙터에서 머티리얼·투명도를 바꾸면 개별 게이트 외형을 갱신한다.
    // (병합 메시는 'Rebuild' 버튼으로 다시 만든다.)
    void OnValidate()
    {
        foreach (var g in FindObjectsByType<ColorGateBlock>(FindObjectsSortMode.None))
            g.ApplyAppearance();
    }

    /// <summary>병합 메시를 제거하고 개별 블록 렌더러를 다시 켠다(편집용).</summary>
    public void ClearPreview()
    {
        var existing = transform.Find("GateMeshes");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }
        foreach (var g in FindObjectsByType<ColorGateBlock>(FindObjectsSortMode.None))
            g.SetRendererEnabled(true);
    }

    Transform PrepareRoot()
    {
        var existing = transform.Find("GateMeshes");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }
        var root = new GameObject("GateMeshes").transform;
        root.SetParent(transform, false);
        root.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // 월드 좌표로 메시 생성
        return root;
    }

    void BuildGroup(Transform parent, Color32 color, List<ColorGateBlock> blocks, Material mat, float alpha)
    {
        var cells = new HashSet<Vector3Int>();
        foreach (var b in blocks) cells.Add(b.GridCell);

        var verts = new List<Vector3>();
        var tris = new List<int>();

        foreach (var cell in cells)
        {
            Vector3 center = new Vector3(cell.x, cell.y + 0.5f, cell.z);
            foreach (var d in Dirs)
                if (!cells.Contains(cell + d)) // 같은 그룹 이웃이 없으면 = 외부 면만 생성
                    AddFace(verts, tris, center, d);
        }

        var mesh = new Mesh { name = $"GateMesh_{color.r}_{color.g}_{color.b}" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject(mesh.name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        if (mat != null) mr.sharedMaterial = mat;

        Color32 c = color;
        c.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", c);
        mr.SetPropertyBlock(mpb);
    }

    // 단위 큐브의 한 면(바깥쪽 법선 dir)을 사각형으로 추가한다.
    static void AddFace(List<Vector3> verts, List<int> tris, Vector3 center, Vector3Int dir)
    {
        Vector3 n = new Vector3(dir.x, dir.y, dir.z);
        Vector3 faceCenter = center + n * 0.5f;

        Vector3 u, v;
        if (dir.x != 0) { u = Vector3.up; v = Vector3.forward; }
        else if (dir.y != 0) { u = Vector3.right; v = Vector3.forward; }
        else { u = Vector3.right; v = Vector3.up; }

        Vector3 a = faceCenter - u * 0.5f - v * 0.5f;
        Vector3 b = faceCenter + u * 0.5f - v * 0.5f;
        Vector3 c = faceCenter + u * 0.5f + v * 0.5f;
        Vector3 d = faceCenter - u * 0.5f + v * 0.5f;

        int i = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);

        // 법선이 바깥(dir)을 향하도록 와인딩 결정
        if (Vector3.Dot(Vector3.Cross(b - a, d - a), n) > 0f)
        {
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }
        else
        {
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
            tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
        }
    }

    // RGB만 비교(알파 무시)하는 Color32 비교자.
    class Color32RGBComparer : IEqualityComparer<Color32>
    {
        public bool Equals(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;
        public int GetHashCode(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;
    }
}
