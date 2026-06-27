#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// [개발자 전용] MazeGenerator 커스텀 에디터.
/// Scene 뷰에서 마인크래프트식으로 블록을 편집한다(클릭 설치 / Shift+클릭 제거).
/// 만든 큐브는 실제 씬 오브젝트라 씬과 함께 저장된다.
/// </summary>
[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    // 컴파일/재선택 후에도 유지되도록 SessionState에 보관
    bool EditMode
    {
        get => SessionState.GetBool("MazeEdit.On", false);
        set => SessionState.SetBool("MazeEdit.On", value);
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
            EditorGUILayout.HelpBox(
                "Scene 뷰에서 클릭 = 블록 설치, Shift+클릭 = 블록 제거.\n" +
                "블록의 면을 보고 클릭하면 그 면 쪽(위/아래/옆, -y 포함)에 놓입니다.",
                MessageType.None);
        }
    }

    void OnSceneGUI()
    {
        if (!EditMode) return;

        var maze = (MazeGenerator)target;
        Event e = Event.current;

        // 클릭이 오브젝트 선택 해제로 새지 않도록 기본 컨트롤 확보
        int id = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);

        // 마우스 위치의 대상 칸 계산 + 미리보기 (항상 위에 그려 가림 방지)
        if (TryGetTargetCell(maze, e.mousePosition, out Vector3 center))
        {
            var prev = Handles.zTest;
            Handles.zTest = CompareFunction.Always;
            Handles.color = e.shift ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(0.3f, 1f, 0.4f, 1f);
            Handles.DrawWireCube(center, Vector3.one * 1.001f);
            Handles.zTest = prev;
        }

        // 미리보기가 마우스를 따라오도록 계속 갱신
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            HandleUtility.Repaint();

        // 좌클릭 = 설치, Shift+좌클릭 = 제거
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            if (e.shift) RemoveAt(maze, e.mousePosition);
            else PlaceAt(maze, center);
            e.Use();
        }
    }

    // 마우스 위치 → 설치될 칸 중심.
    // 블록에 맞은 면 쪽 이웃 칸(위/아래/옆 모두, -y 포함). 빈 공간이면 대상 없음.
    bool TryGetTargetCell(MazeGenerator maze, Vector2 mousePos, out Vector3 center)
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
            return true;
        }

        center = default;
        return false;
    }

    // 씬에 블록이 하나도 없으면 (0, -0.5, 0)에 시작 블록 하나를 만든다.
    void EnsureSeedBlock(MazeGenerator maze)
    {
        var root = GetMazeRoot(maze);
        foreach (Transform child in root)
            if (child.name == "Block") return;

        CreateBlock(maze, new Vector3(0f, -0.5f, 0f));
    }

    void PlaceAt(MazeGenerator maze, Vector3 center)
    {
        // 같은 칸 중복 방지
        foreach (var h in Physics.OverlapBox(center, Vector3.one * 0.4f))
            if (h.gameObject.name == "Block") return;

        CreateBlock(maze, center);
    }

    void CreateBlock(MazeGenerator maze, Vector3 center)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Block";
        Undo.RegisterCreatedObjectUndo(go, "Place Block");
        go.transform.SetParent(GetMazeRoot(maze), true);
        go.transform.position = center;
        go.transform.localScale = Vector3.one;
        MarkDirty(maze);
    }

    void RemoveAt(MazeGenerator maze, Vector2 mousePos)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.gameObject.name == "Block")
        {
            Undo.DestroyObjectImmediate(hit.collider.gameObject);
            MarkDirty(maze);
        }
    }

    static Transform GetMazeRoot(MazeGenerator maze)
    {
        var existing = maze.transform.Find("Maze");
        if (existing != null) return existing;

        var root = new GameObject("Maze");
        Undo.RegisterCreatedObjectUndo(root, "Create Maze Root");
        root.transform.SetParent(maze.transform, false);
        return root.transform;
    }

    static void MarkDirty(MazeGenerator maze)
    {
        EditorUtility.SetDirty(maze);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(maze.gameObject.scene);
    }
}
#endif
