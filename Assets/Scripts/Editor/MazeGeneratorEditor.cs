#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// [개발자 전용] MazeGenerator 커스텀 에디터.
/// Scene 뷰에서 마인크래프트식으로 블록을 편집한다(클릭 설치 / Shift+클릭 제거).
/// 인스펙터에서 프리팹을 지정하면 기본 블록 대신 그 프리팹(컬러 필터 등 기물)을 설치할 수 있다.
/// 만든 오브젝트는 실제 씬 오브젝트라 씬과 함께 저장된다.
/// </summary>
[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    // 특수블록(필터+기물) 전용 폴더 이름. 일반 벽 블록은 이 폴더에 들어가지 않고 기존처럼 Maze 루트에 바로 놓인다.
    const string MapObjectsFolderName = "MapObjects";

    // 컴파일/재선택 후에도 유지되도록 SessionState에 보관
    bool EditMode
    {
        get => SessionState.GetBool("MazeEdit.On", false);
        set => SessionState.SetBool("MazeEdit.On", value);
    }

    // 클릭/드래그로 설치할 프리팹의 애셋 경로. 비어있으면 기본 블록(큐브)을 설치한다.
    string SelectedPrefabPath
    {
        get => SessionState.GetString("MazeEdit.PrefabPath", "");
        set => SessionState.SetString("MazeEdit.PrefabPath", value);
    }

    // 컬러 필터 설치 시 미리 지정해둘 R/G/B 값.
    int PresetRed
    {
        get => SessionState.GetInt("MazeEdit.ColorFilter.R", 0);
        set => SessionState.SetInt("MazeEdit.ColorFilter.R", value);
    }
    int PresetGreen
    {
        get => SessionState.GetInt("MazeEdit.ColorFilter.G", 0);
        set => SessionState.SetInt("MazeEdit.ColorFilter.G", value);
    }
    int PresetBlue
    {
        get => SessionState.GetInt("MazeEdit.ColorFilter.B", 0);
        set => SessionState.SetInt("MazeEdit.ColorFilter.B", value);
    }

    // RGB 필터 설치 시 미리 지정해둘 목표 색.
    LightColor PresetTargetColor
    {
        get => (LightColor)SessionState.GetInt("MazeEdit.RgbFilter.Target", 0);
        set => SessionState.SetInt("MazeEdit.RgbFilter.Target", (int)value);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var maze = (MazeGenerator)target;
        EditorGUILayout.Space();

        // 블록 편집 모드 토글 (켜는 순간 블록이 없으면 시드 블록 생성)
        bool now = GUILayout.Toggle(EditMode, "Scene 뷰 블록 편집 모드", "Button");
        if (now && !EditMode) EnsureSeedBlock(maze);
        EditMode = now;

        if (EditMode)
        {
            // 설치할 프리팹 선택 (컬러 필터·RGB 필터 등 기물 프리팹도 이 칸에 끌어넣어 바로 소환 가능).
            GameObject currentPrefab = string.IsNullOrEmpty(SelectedPrefabPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(SelectedPrefabPath);
            GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(
                "설치할 프리팹 (비우면 기본 블록)", currentPrefab, typeof(GameObject), false);
            SelectedPrefabPath = newPrefab != null ? AssetDatabase.GetAssetPath(newPrefab) : "";

            // 자주 쓰는 3종류(일반 블록/컬러 필터/RGB 필터)는 라디오 버튼으로 바로 고를 수 있다.
            // 버튼을 누르면 위 프리팹 칸이 그 프리팹으로 바뀌는 것과 같다(같은 SelectedPrefabPath를 공유).
            bool isBasic = string.IsNullOrEmpty(SelectedPrefabPath);
            bool isColorFilter = maze.colorFilterPrefab != null
                && SelectedPrefabPath == AssetDatabase.GetAssetPath(maze.colorFilterPrefab);
            bool isRgbFilter = maze.rgbFilterPrefab != null
                && SelectedPrefabPath == AssetDatabase.GetAssetPath(maze.rgbFilterPrefab);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(isBasic, "일반 블록", "Button")) SelectedPrefabPath = "";
            using (new EditorGUI.DisabledScope(maze.colorFilterPrefab == null))
                if (GUILayout.Toggle(isColorFilter, "컬러 필터", "Button"))
                    SelectedPrefabPath = AssetDatabase.GetAssetPath(maze.colorFilterPrefab);
            using (new EditorGUI.DisabledScope(maze.rgbFilterPrefab == null))
                if (GUILayout.Toggle(isRgbFilter, "RGB 필터", "Button"))
                    SelectedPrefabPath = AssetDatabase.GetAssetPath(maze.rgbFilterPrefab);
            GUILayout.EndHorizontal();

            if (isColorFilter)
            {
                EditorGUILayout.LabelField("컬러 필터 값 (설치 시 적용)", EditorStyles.boldLabel);
                PresetRed = EditorGUILayout.IntField("R", PresetRed);
                PresetGreen = EditorGUILayout.IntField("G", PresetGreen);
                PresetBlue = EditorGUILayout.IntField("B", PresetBlue);
            }
            else if (isRgbFilter)
            {
                EditorGUILayout.LabelField("RGB 필터 값 (설치 시 적용)", EditorStyles.boldLabel);
                PresetTargetColor = (LightColor)EditorGUILayout.EnumPopup("목표 색", PresetTargetColor);
            }

            EditorGUILayout.HelpBox(
                "Scene 뷰에서 클릭 = 설치, Shift+클릭 = 제거.\n" +
                "블록의 면을 보고 클릭하면 그 면 쪽(위/아래/옆, -y 포함)에 놓입니다.\n" +
                "Ctrl+드래그 = 직사각형 범위 설치, Shift+Ctrl+드래그 = 직사각형 범위 제거.\n" +
                "위 프리팹 칸에 기물 프리팹을 지정하면 기본 블록 대신 그 프리팹이 설치됩니다.",
                MessageType.None);
        }
    }

    // 사각형 드래그 배치 상태
    bool dragging;
    bool dragRemove;
    int dragAxis; // 드래그 평면의 고정축 (0=x, 1=y, 2=z)
    Vector3 dragStartCell;
    Vector3 dragEndCell;

    void OnSceneGUI()
    {
        if (!EditMode) return;

        var maze = (MazeGenerator)target;
        Event e = Event.current;

        // 클릭이 오브젝트 선택 해제로 새지 않도록 기본 컨트롤 확보
        int id = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);

        if (dragging)
        {
            UpdateDragRect(e.mousePosition);
            DrawDragPreview();

            if (e.type == EventType.MouseDrag) HandleUtility.Repaint();

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                CommitDragRect(maze);
                dragging = false;
                e.Use();
            }
            return;
        }

        // 마우스 위치의 대상 칸 계산 + 미리보기 (다른 블록에 가려지는 모서리는 그리지 않음)
        bool hasTarget = TryGetTargetCell(maze, e.mousePosition, out Vector3 center, out Vector3 normal);
        if (hasTarget)
        {
            var prev = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = e.shift ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(0.3f, 1f, 0.4f, 1f);
            Handles.DrawWireCube(center, Vector3.one * 1.001f);
            Handles.zTest = prev;
        }

        // 미리보기가 마우스를 따라오도록 계속 갱신
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            HandleUtility.Repaint();

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            // Ctrl+클릭 시작 = 직사각형 드래그 배치/제거 시작
            if (e.control && hasTarget)
            {
                dragging = true;
                dragRemove = e.shift;
                dragAxis = DominantAxis(normal);

                // 고정축은 클릭한 면 쪽 이웃 칸이 아니라, 실제로 클릭한 블록 자신의 칸으로 맞춘다.
                // (Y면을 클릭해 드래그하면 한 층 위/아래가 아니라 같은 층에서 옆으로 채워지도록)
                Vector3 startCell = center;
                if (dragAxis == 0) startCell.x -= normal.x;
                else if (dragAxis == 1) startCell.y -= normal.y;
                else startCell.z -= normal.z;

                dragStartCell = startCell;
                dragEndCell = startCell;
                e.Use();
                return;
            }

            // 좌클릭 = 설치, Shift+좌클릭 = 제거
            if (e.shift) RemoveAt(maze, e.mousePosition);
            else if (hasTarget) PlaceAt(maze, center);
            e.Use();
        }
    }

    // 마우스 위치 → 설치될 칸 중심 + 맞은 면의 법선.
    // 블록에 맞은 면 쪽 이웃 칸(위/아래/옆 모두, -y 포함). 빈 공간이면 대상 없음.
    bool TryGetTargetCell(MazeGenerator maze, Vector2 mousePos, out Vector3 center, out Vector3 normal)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            // 맞은 점에서 법선 방향으로 반 칸 이동 후 그리드에 스냅.
            // 칸 중심 규칙: x,z = 정수,  y = 정수 + 0.5
            float px = hit.point.x + hit.normal.x * 0.5f;
            float py = hit.point.y + hit.normal.y * 0.5f;
            float pz = hit.point.z + hit.normal.z * 0.5f;
            center = new Vector3(
                Mathf.RoundToInt(px),
                Mathf.Round(py - 0.5f) + 0.5f,
                Mathf.RoundToInt(pz));
            normal = hit.normal;
            return true;
        }

        center = default;
        normal = default;
        return false;
    }

    // 법선에서 가장 지배적인 축(드래그 평면의 고정축)을 고른다.
    static int DominantAxis(Vector3 normal)
    {
        float ax = Mathf.Abs(normal.x), ay = Mathf.Abs(normal.y), az = Mathf.Abs(normal.z);
        if (ax >= ay && ax >= az) return 0;
        if (ay >= ax && ay >= az) return 1;
        return 2;
    }

    static Vector3 AxisVector(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

    // 드래그 시작 칸을 지나는 고정축 평면과 마우스 레이의 교점으로 반대쪽 끝 칸을 갱신.
    void UpdateDragRect(Vector2 mousePos)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        var plane = new Plane(AxisVector(dragAxis), dragStartCell);
        if (!plane.Raycast(ray, out float dist)) return;

        Vector3 p = ray.GetPoint(dist);
        float x = dragAxis == 0 ? dragStartCell.x : Mathf.RoundToInt(p.x);
        float y = dragAxis == 1 ? dragStartCell.y : Mathf.Round(p.y - 0.5f) + 0.5f;
        float z = dragAxis == 2 ? dragStartCell.z : Mathf.RoundToInt(p.z);
        dragEndCell = new Vector3(x, y, z);
    }

    // 시작/끝 칸 사이의 직사각형 범위에 속한 모든 칸 중심 목록.
    List<Vector3> GetDragCells()
    {
        var cells = new List<Vector3>();

        float minX = Mathf.Min(dragStartCell.x, dragEndCell.x), maxX = Mathf.Max(dragStartCell.x, dragEndCell.x);
        float minY = Mathf.Min(dragStartCell.y, dragEndCell.y), maxY = Mathf.Max(dragStartCell.y, dragEndCell.y);
        float minZ = Mathf.Min(dragStartCell.z, dragEndCell.z), maxZ = Mathf.Max(dragStartCell.z, dragEndCell.z);

        for (float x = minX; x <= maxX + 0.01f; x += 1f)
            for (float y = minY; y <= maxY + 0.01f; y += 1f)
                for (float z = minZ; z <= maxZ + 0.01f; z += 1f)
                    cells.Add(new Vector3(x, y, z));

        return cells;
    }

    void DrawDragPreview()
    {
        var prev = Handles.zTest;
        Handles.zTest = CompareFunction.LessEqual;
        Handles.color = dragRemove ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(0.3f, 1f, 0.4f, 1f);
        foreach (var cell in GetDragCells())
            Handles.DrawWireCube(cell, Vector3.one * 1.001f);
        Handles.zTest = prev;
    }

    void CommitDragRect(MazeGenerator maze)
    {
        var cells = GetDragCells();
        Undo.SetCurrentGroupName(dragRemove ? "Remove Blocks (Rect)" : "Place Blocks (Rect)");
        int group = Undo.GetCurrentGroup();

        foreach (var cell in cells)
        {
            if (dragRemove) RemoveCell(maze, cell);
            else PlaceAt(maze, cell);
        }

        Undo.CollapseUndoOperations(group);
    }

    // 좌표 기준 오브젝트 제거(드래그 범위 제거용, 마우스 레이캐스트 없이 칸 위치로 직접 찾는다).
    void RemoveCell(MazeGenerator maze, Vector3 center)
    {
        var root = GetMazeRoot(maze);
        var mapObjects = FindMapObjectsFolder();
        foreach (var h in Physics.OverlapBox(center, Vector3.one * 0.4f))
        {
            var placed = FindPlacedObject(h.transform, root, mapObjects);
            if (placed != null)
            {
                Undo.DestroyObjectImmediate(placed);
                ReorganizeFilterFoldersIfPresent();
                MarkDirty(maze);
                return;
            }
        }
    }

    // 씬에 설치된 오브젝트가 하나도 없으면 (0, -0.5, 0)에 시작 블록 하나를 만든다.
    void EnsureSeedBlock(MazeGenerator maze)
    {
        var root = GetMazeRoot(maze);
        if (root.childCount > 0) return;

        CreateBlock(maze, new Vector3(0f, -0.5f, 0f));
    }

    void PlaceAt(MazeGenerator maze, Vector3 center)
    {
        var root = GetMazeRoot(maze);
        var mapObjects = FindMapObjectsFolder();

        // 같은 칸 중복 방지
        foreach (var h in Physics.OverlapBox(center, Vector3.one * 0.4f))
            if (FindPlacedObject(h.transform, root, mapObjects) != null) return;

        CreateBlock(maze, center);
    }

    void CreateBlock(MazeGenerator maze, Vector3 center)
    {
        var root = GetMazeRoot(maze);
        string prefabPath = SelectedPrefabPath;
        var prefabAsset = string.IsNullOrEmpty(prefabPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        GameObject go;
        if (prefabAsset != null)
        {
            // 특수블록(필터+기물)은 Maze가 아니라 씬 바로 아래 MapObjects 폴더에 놓는다.
            // 필터는 그중에서도 메시를 공유하는(같은 색+인접) 것끼리 RGBFilterN 하위 폴더로 다시 묶는다.
            Transform mapObjects = GetOrCreateMapObjectsFolder();

            go = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, mapObjects);
            Undo.RegisterCreatedObjectUndo(go, "Place Prefab");
            go.transform.position = center;
            ApplyFilterPreset(go);

            if (prefabAsset.GetComponent<FilterBlockBase>() != null)
            {
                FilterClusterOrganizer.Reorganize(mapObjects);
                FilterBlockBase.RebuildAll(); // 새로 설치한 필터를 포함해 병합 메시를 바로 갱신
            }
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Block";
            Undo.RegisterCreatedObjectUndo(go, "Place Block");
            go.transform.SetParent(root, true);
            go.transform.position = center;
            go.transform.localScale = Vector3.one;
        }

        MarkDirty(maze);
    }

    // 방금 설치한 오브젝트가 컬러 필터/RGB 필터면, 라디오 버튼 아래에서 미리 지정해둔 값을 채워 넣는다.
    // private [SerializeField] 필드라 SerializedObject로 값을 써야 한다.
    void ApplyFilterPreset(GameObject go)
    {
        if (go.TryGetComponent<ColorFilterBlock>(out var colorFilter))
        {
            var so = new SerializedObject(colorFilter);
            so.FindProperty("red").intValue = PresetRed;
            so.FindProperty("green").intValue = PresetGreen;
            so.FindProperty("blue").intValue = PresetBlue;
            so.ApplyModifiedProperties();
        }
        else if (go.TryGetComponent<RgbFilterBlock>(out var rgbFilter))
        {
            var so = new SerializedObject(rgbFilter);
            so.FindProperty("targetColor").enumValueIndex = (int)PresetTargetColor;
            so.ApplyModifiedProperties();
        }
    }

    void RemoveAt(MazeGenerator maze, Vector2 mousePos)
    {
        var root = GetMazeRoot(maze);
        var mapObjects = FindMapObjectsFolder();
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            var placed = FindPlacedObject(hit.collider.transform, root, mapObjects);
            if (placed != null)
            {
                Undo.DestroyObjectImmediate(placed);
                ReorganizeFilterFoldersIfPresent();
                MarkDirty(maze);
            }
        }
    }

    // 맞은 콜라이더가 어떤 설치된 오브젝트(일반 블록이든 특수블록이든)에 속하는지 찾는다.
    // 일반 블록은 Maze 루트 바로 아래, 특수블록(필터+기물)은 씬 바로 아래 MapObjects 폴더 바로 아래에 있고,
    // 필터는 그 안의 RGBFilterN 폴더 밑에 한 단계 더 들어가 있으므로 컨테이너를 만날 때마다 한 단계씩 더 내려간다.
    static GameObject FindPlacedObject(Transform hit, Transform root, Transform mapObjects)
    {
        var t = hit;
        while (t != null && t.parent != root && (mapObjects == null || t.parent != mapObjects))
            t = t.parent;

        if (t == null) return null;

        if (t.parent == root) return t.gameObject; // 일반 블록

        if (mapObjects != null && t.parent == mapObjects)
        {
            while (IsContainerFolder(t))
            {
                var inner = hit;
                while (inner != null && inner.parent != t) inner = inner.parent;
                if (inner == null || inner.parent != t) return null;
                t = inner;
            }
            return t.gameObject;
        }

        return null;
    }

    static bool IsContainerFolder(Transform t) => FilterClusterOrganizer.IsClusterFolder(t);

    static Transform GetMazeRoot(MazeGenerator maze)
    {
        var existing = maze.transform.Find("Maze");
        if (existing != null) return existing;

        var root = new GameObject("Maze");
        Undo.RegisterCreatedObjectUndo(root, "Create Maze Root");
        root.transform.SetParent(maze.transform, false);
        return root.transform;
    }

    // MapObjects는 Maze의 자식이 아니라 씬 바로 아래(루트)에 독립적으로 존재한다.
    static Transform FindMapObjectsFolder()
    {
        var existing = GameObject.Find(MapObjectsFolderName);
        return existing != null ? existing.transform : null;
    }

    static Transform GetOrCreateMapObjectsFolder()
    {
        var existing = FindMapObjectsFolder();
        if (existing != null) return existing;

        var folder = new GameObject(MapObjectsFolderName);
        Undo.RegisterCreatedObjectUndo(folder, "Create MapObjects Folder");
        return folder.transform;
    }

    // 필터 제거 후 폴더 재구성 + 병합 메시 갱신을 함께 처리한다.
    // (지운 오브젝트가 필터가 아니었어도 호출은 되지만, 필터가 없으면 아무 폴더/메시도 안 건드리므로 무해하다.)
    static void ReorganizeFilterFoldersIfPresent()
    {
        var mapObjects = FindMapObjectsFolder();
        if (mapObjects != null) FilterClusterOrganizer.Reorganize(mapObjects);
        FilterBlockBase.RebuildAll(); // 제거로 인해 사라진 필터를 반영해 병합 메시를 바로 갱신
    }

    static void MarkDirty(MazeGenerator maze)
    {
        EditorUtility.SetDirty(maze);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(maze.gameObject.scene);
    }
}
#endif
