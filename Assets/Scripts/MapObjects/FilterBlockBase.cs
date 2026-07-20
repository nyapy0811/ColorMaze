using System.Collections.Generic;
using Framework.Core;
using UnityEngine;

/// <summary>
/// 판정형(필터) 기물 공통 베이스(4.2 컬러 필터, 4.3 RGB 필터).
/// 플레이어의 스택이 바뀔 때(ColorStackChanged) 또는 스테이지가 시작될 때(SceneLoadCompleted)만
/// Matches()로 통과 가능 여부를 다시 판정해 콜라이더를 트리거/솔리드로 토글하고
/// 채움 메시 투명도를 갱신한다(매 프레임 폴링하지 않음).
/// 플레이어가 완전히 통과하면 스택을 초기화한다.
/// 같은 외형 색을 가진 필터들끼리 채움(fill)/테두리(border) 메시로 자동 병합한다.
/// 별도의 매니저 컴포넌트 없이 필터 스크립트만 씬에 있으면 동작한다.
/// </summary>
public abstract class FilterBlockBase : MapObjectBase
{
    [Header("외형 (같은 색 필터끼리 공유하는 병합 메시 설정)")]
    [Tooltip("필터에 적용할 머티리얼(Transparent로 만들어 둔 것)")]
    public Material gateMaterial;

    [Tooltip("채움(안쪽) 색의 투명도(0 = 완전 투명, 1 = 불투명)")]
    [Range(0f, 1f)] public float gateAlpha = 0.5f;

    [Tooltip("테두리 두께(면 크기 대비 비율, 0~0.5). 0이면 테두리 없음")]
    [Range(0f, 0.5f)] public float borderWidth = 0.02f;

    [Tooltip("테두리 색의 불투명도(0 = 완전 투명, 1 = 불투명)")]
    [Range(0f, 1f)] public float borderAlpha = 1f;

    bool passing;
    MeshRenderer fillRenderer; // 이 블록이 속한 그룹의 채움(fill) 메시 렌더러(테두리 제외, 그룹 전체가 공유)
    float builtFillAlpha; // 통과 불가능할 때 되돌아갈 원래 채움 투명도

    /// <summary>현재 플레이어 상태로 통과 가능한지 판정한다. 하위 클래스가 구현한다.</summary>
    protected abstract bool Matches(ColorStacks player);

    /// <summary>이 게이트의 외형/그룹핑에 쓸 색상. 하위 클래스가 구현한다.</summary>
    protected abstract Color32 GetAppearanceColor();

    /// <summary>그룹(같은 색 필터 전체)에 하나만 띄울 라벨 텍스트. null/빈 문자열을 반환하면 라벨을 만들지 않는다. 하위 클래스가 구현한다.</summary>
    protected abstract string GetLabelText();

    /// <summary>이 게이트의 외형 색(메시 병합 시 그룹 키로도 쓰인다).</summary>
    public Color32 RGB => GetAppearanceColor();

    /// <summary>정수 그리드 칸 좌표. 블록 중심 = (x, y+0.5, z).</summary>
    public Vector3Int GridCell => new Vector3Int(
        Mathf.RoundToInt(transform.position.x),
        Mathf.RoundToInt(transform.position.y - 0.5f),
        Mathf.RoundToInt(transform.position.z));

    /// <summary>개별 렌더러 표시 여부(병합 메시가 대신 그릴 때 끔).</summary>
    public void SetRendererEnabled(bool enabled)
    {
        if (TryGetComponent<Renderer>(out var r)) r.enabled = enabled;
    }

    protected virtual void Start()
    {
        RebuildAll();
        Refresh(); // fillRenderer가 이제 막 배정됐으니 초기 상태를 한 번 반영
    }

    void OnEnable()
    {
        EventBus.Subscribe<ColorStackChanged>(OnStackChanged);
        EventBus.Subscribe<SceneLoadCompleted>(OnStageStart);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<ColorStackChanged>(OnStackChanged);
        EventBus.Unsubscribe<SceneLoadCompleted>(OnStageStart);
    }

    void OnStackChanged(ColorStackChanged e) => Refresh();

    // 스테이지(씬)가 로드 완료됐을 때도 한 번 판정한다 — Start() 시점엔 플레이어 스택이
    // 아직 이번 스테이지용으로 정리되지 않았을 수 있어서다.
    void OnStageStart(SceneLoadCompleted e) => Refresh();

    // 통과 가능 여부를 다시 판정해 콜라이더와 채움 메시 투명도를 갱신한다.
    void Refresh()
    {
        if (Player == null) return;
        bool matches = Matches(Player);
        Col.isTrigger = matches;
        ApplyFillTransparency(matches);
    }

    // 통과 가능하면 채움(fill) 메시를 투명하게, 아니면 원래 투명도로 되돌린다(테두리는 건드리지 않음).
    void ApplyFillTransparency(bool passable)
    {
        if (fillRenderer == null) return;

        var mpb = new MaterialPropertyBlock();
        fillRenderer.GetPropertyBlock(mpb);
        Color32 c = GetAppearanceColor();
        c.a = (byte)Mathf.RoundToInt((passable ? 0f : builtFillAlpha) * 255f);
        mpb.SetColor("_BaseColor", c);
        fillRenderer.SetPropertyBlock(mpb);
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other)) passing = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!passing || !IsPlayer(other)) return;
        passing = false;
        Player.ResetAll(); // 통과 완료 → 스택 초기화
    }

    // 인스펙터에서 값을 바꾸면(에디터 미리보기) 씬 전체를 다시 병합한다.
    // OnValidate 도중 바로 RebuildAll()을 실행하면 TMP가 라벨 생성 중 DestroyImmediate를
    // 호출해 경고가 뜨므로, 한 프레임 뒤로 미뤄서 실행한다.
#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return; // 미뤄지는 동안 오브젝트가 파괴됐을 수 있음
            RebuildAll();
        };
    }
#endif

    // ---------- 메시 병합 (구 GateMeshCombiner를 흡수) ----------

    static readonly Vector3Int[] Dirs =
    {
        new(1, 0, 0), new(-1, 0, 0),
        new(0, 1, 0), new(0, -1, 0),
        new(0, 0, 1), new(0, 0, -1),
    };

    static Transform meshRoot;

    /// <summary>씬의 모든 FilterBlockBase(컬러 필터·RGB 필터)를 같은 색끼리, 그중에서도 실제로 맞닿아
    /// 이어진 덩어리끼리만 묶어 다시 병합한다(멀리 떨어진 같은 색 블록은 서로 다른 그룹으로 취급).</summary>
    public static void RebuildAll()
    {
        var root = PrepareRoot();

        var gates = FindObjectsByType<FilterBlockBase>(FindObjectsSortMode.None);
        var byColor = new Dictionary<Color32, List<FilterBlockBase>>(new Color32RGBComparer());

        foreach (var g in gates)
        {
            g.SetRendererEnabled(false); // 개별 렌더 끄고 병합 메시로 표시
            if (!byColor.TryGetValue(g.RGB, out var list))
                byColor[g.RGB] = list = new List<FilterBlockBase>();
            list.Add(g);
        }

        foreach (var kv in byColor)
            foreach (var cluster in SplitConnected(kv.Value))
                BuildGroup(root, kv.Key, cluster);
    }

    // 같은 색이라도 물리적으로 붙어있지 않은 덩어리는 서로 다른 그룹으로 나눈다(6방향 연결 기준 flood fill).
    // 이렇게 해야 서로 다른 위치의 같은 색 필터가 하나의 메시/라벨로 뒤섞이지 않는다.
    static List<List<FilterBlockBase>> SplitConnected(List<FilterBlockBase> blocks)
    {
        var byCell = new Dictionary<Vector3Int, FilterBlockBase>();
        foreach (var b in blocks) byCell[b.GridCell] = b;

        var visited = new HashSet<Vector3Int>();
        var clusters = new List<List<FilterBlockBase>>();

        foreach (var b in blocks)
        {
            var startCell = b.GridCell;
            if (!visited.Add(startCell)) continue;

            var cluster = new List<FilterBlockBase>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(startCell);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                cluster.Add(byCell[cell]);
                foreach (var d in Dirs)
                {
                    var next = cell + d;
                    if (byCell.ContainsKey(next) && visited.Add(next))
                        queue.Enqueue(next);
                }
            }
            clusters.Add(cluster);
        }
        return clusters;
    }

    static Transform PrepareRoot()
    {
        if (meshRoot == null)
        {
            var existing = GameObject.Find("GateMeshes");
            meshRoot = existing != null ? existing.transform : new GameObject("GateMeshes").transform;
        }

        for (int i = meshRoot.childCount - 1; i >= 0; i--)
        {
            var child = meshRoot.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        return meshRoot;
    }

    // 면을 채움(fill)과 테두리(border) 두 개의 메시로 나눠 만든다.
    // 서로 겹치지 않게 안쪽 사각형(채움)과 그 바깥 테(테두리)로 정확히 나눠 붙이므로 Z-fighting이 없다.
    // 그룹의 외형 설정(머티리얼·알파·테두리 두께)은 그룹 내 첫 블록의 값을 사용한다.
    static void BuildGroup(Transform parent, Color32 color, List<FilterBlockBase> blocks)
    {
        var settings = blocks[0];

        var cells = new HashSet<Vector3Int>();
        foreach (var b in blocks) cells.Add(b.GridCell);

        var fillVerts = new List<Vector3>();
        var fillTris = new List<int>();
        var borderVerts = new List<Vector3>();
        var borderTris = new List<int>();

        float b0 = Mathf.Clamp(settings.borderWidth, 0f, 0.5f);
        float inner = 0.5f - b0;

        foreach (var cell in cells)
        {
            Vector3 center = new Vector3(cell.x, cell.y + 0.5f, cell.z);
            foreach (var d in Dirs)
            {
                if (cells.Contains(cell + d)) continue; // 같은 그룹 이웃이 있으면 내부 면 → 생성 안 함

                Vector3 n = new Vector3(d.x, d.y, d.z);
                Vector3 faceCenter = center + n * 0.5f;
                Vector3 u, v;
                if (d.x != 0) { u = Vector3.up; v = Vector3.forward; }
                else if (d.y != 0) { u = Vector3.right; v = Vector3.forward; }
                else { u = Vector3.right; v = Vector3.up; }

                Vector3Int uInt = Vector3Int.RoundToInt(u);
                Vector3Int vInt = Vector3Int.RoundToInt(v);

                // 이웃 칸이 같은 그룹에 속하고 그 이웃도 같은 방향으로 노출된 면을 갖는지(=병합면 내부 경계인지) 판정.
                bool Interior(Vector3Int edge) => cells.Contains(cell + edge) && !cells.Contains(cell + edge + d);

                bool bR = !Interior(uInt);
                bool bL = !Interior(-uInt);
                bool bT = !Interior(vInt);
                bool bB = !Interior(-vInt);

                float uMinFill = bL ? -inner : -0.5f;
                float uMaxFill = bR ? inner : 0.5f;
                float vMinFill = bB ? -inner : -0.5f;
                float vMaxFill = bT ? inner : 0.5f;

                // 채움: 통합 메시의 바깥 경계에서만 안쪽으로 들어가고, 같은 그룹과 맞닿은 내부 경계는 끝까지 채운다.
                AddQuad(fillVerts, fillTris, faceCenter, n, u, v, uMinFill, uMaxFill, vMinFill, vMaxFill);

                if (b0 > 0f)
                {
                    // 테두리는 통합 메시의 바깥 경계(boundary)에만 그린다 — 내부 경계는 건너뛴다.
                    // 위/아래 띠는 전체 너비, 좌/우 띠는 위/아래 띠 사이 높이로 제한해 모서리 겹침 없이 이어붙인다.
                    if (bT) AddQuad(borderVerts, borderTris, faceCenter, n, u, v, -0.5f, 0.5f, inner, 0.5f);
                    if (bB) AddQuad(borderVerts, borderTris, faceCenter, n, u, v, -0.5f, 0.5f, -0.5f, -inner);
                    if (bL) AddQuad(borderVerts, borderTris, faceCenter, n, u, v, -0.5f, -inner, bB ? -inner : -0.5f, bT ? inner : 0.5f);
                    if (bR) AddQuad(borderVerts, borderTris, faceCenter, n, u, v, inner, 0.5f, bB ? -inner : -0.5f, bT ? inner : 0.5f);
                }
            }
        }

        string baseName = $"GateMesh_{color.r}_{color.g}_{color.b}";
        var fillGo = BuildMeshObject(parent, baseName + "_Fill", fillVerts, fillTris, settings.gateMaterial, color, settings.gateAlpha);
        var fillRenderer = fillGo.GetComponent<MeshRenderer>();
        foreach (var b in blocks)
        {
            b.fillRenderer = fillRenderer;
            b.builtFillAlpha = settings.gateAlpha;
        }

        if (b0 > 0f)
            BuildMeshObject(parent, baseName + "_Border", borderVerts, borderTris, BorderMaterial(settings.gateMaterial), color, settings.borderAlpha);

        // 그룹당 하나만: 라벨 텍스트가 있는 그룹(대표 블록 기준)에만 라벨을 만든다.
        string labelText = settings.GetLabelText();
        if (!string.IsNullOrEmpty(labelText))
            BuildGroupLabel(parent, baseName + "_Label", cells, labelText);
    }

    // 그룹의 칸(셀) 위에 라벨 하나를 띄운다. 실제 위치·회전 계산은 범용 컴포넌트 CellGroupLabel이 담당한다.
    static void BuildGroupLabel(Transform parent, string name, HashSet<Vector3Int> cells, string text)
    {
        var cellArray = new Vector3Int[cells.Count];
        cells.CopyTo(cellArray);
        CellGroupLabel.Create(parent, name, cellArray, text);
    }

    static Material borderMat;

    // 테두리 전용 머티리얼(깊이 테스트를 항상 통과해 가려진 모서리도 보이게 함). 셰이더가 없으면 채움과 같은 머티리얼로 대체.
    static Material BorderMaterial(Material fallback)
    {
        if (borderMat == null)
        {
            var shader = Shader.Find("Custom/FilterBorderAlwaysVisible");
            if (shader != null) borderMat = new Material(shader);
        }
        return borderMat != null ? borderMat : fallback;
    }

    static GameObject BuildMeshObject(Transform parent, string name, List<Vector3> verts, List<int> tris, Material mat, Color32 color, float alpha)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        if (mat != null) mr.sharedMaterial = mat;

        Color32 c = color;
        c.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", c);
        mr.SetPropertyBlock(mpb);

        return go;
    }

    // 단위 큐브의 한 면 위, (u,v) 로컬 좌표 [uMin,uMax]×[vMin,vMax] 범위의 사각형을 추가한다.
    static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 faceCenter, Vector3 n, Vector3 u, Vector3 v,
        float uMin, float uMax, float vMin, float vMax)
    {
        Vector3 a = faceCenter + u * uMin + v * vMin;
        Vector3 b = faceCenter + u * uMax + v * vMin;
        Vector3 c = faceCenter + u * uMax + v * vMax;
        Vector3 d = faceCenter + u * uMin + v * vMax;

        int i = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);

        // 법선이 바깥(n)을 향하도록 와인딩 결정
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
